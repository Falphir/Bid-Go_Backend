using Bid_Go_Backend.Controllers;
using Bid_Go_Backend.Data;
using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.DTOs;
using Bid_Go_Backend.Data.Models.Enums;
using Bid_Go_Backend.Repositories.Authorization;
using Bid_Go_Backend.Repositories.Payments;
using Bid_Go_Backend.Repositories.Transport_Request;
using Bid_Go_Backend.Services;
using Bid_Go_Backend.Services.Interfaces;
using Bid_Go_Backend.Services.Payments;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Security.Claims;
using Xunit;

namespace Bid_Go.Tests.Integration
{
    /// <summary>
    /// Integration tests for PaymentsController covering process, retry and listing flows.
    /// </summary>
    public class PaymentsControllerTests
    {
        private static (PaymentsController controller, BidGoDbContext db, Mock<IPaymentGateway> gatewayMock, TestNotificationService notifications) BuildAsCompany(int companyId)
        {
            var options = new DbContextOptionsBuilder<BidGoDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            var db = new BidGoDbContext(options);
            var paymentsRepo = new PaymentRepository(db);
            var bidsRepo = new Bid_Go_Backend.Repositories.Bids.BidsRepository(db);
            var bidsService = new BidsService(bidsRepo, db);
            var trRepo = new TransportRequestRepository(db);
            var gatewayMock = new Mock<IPaymentGateway>();
            var notifications = new TestNotificationService();
            var paymentService = new PaymentService(paymentsRepo, bidsService, trRepo, notifications, gatewayMock.Object);
            var authz = new AuthorizationService(new AuthorizationRepository(db));
            var controller = new PaymentsController(paymentService, authz);

            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim("userId", companyId.ToString()),
                new Claim("userType", "Company")
            }, "TestAuth"));

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };

            return (controller, db, gatewayMock, notifications);
        }

        private static (Company company, Driver driver, TransportRequest tr, Bid bid) SeedSelectedBid(BidGoDbContext db)
        {
            var company = new Company
            {
                Name = "Comp",
                CompanyName = "Comp Lda",
                Address = "A",
                Email = "c@x.com",
                Password = "x",
                PhoneNumber = 900000000,
                NIF = 111111111
            };

            var driver = new Driver
            {
                Name = "D",
                Email = "d@x.com",
                Password = "x",
                PhoneNumber = 911111111,
                NIF = 222222222
            };

            db.Companies.Add(company);
            db.Drivers.Add(driver);
            db.SaveChanges();

            var tr = new TransportRequest
            {
                CompanyId = company.Id,
                Status = ERequestStatus.Pending,
                Origin = "O",
                Destination = "D",
                Package = "P",
                Weight = 1,
                Volume = 1,
                Length = 1,
                Width = 1,
                Height = 1,
                PickupDate = DateTime.UtcNow,
                DeliveryDate = DateTime.UtcNow.AddDays(3),
                Image = "img",
                MaxPrice = 100,
                BiddingStartDate = DateTime.UtcNow.AddDays(-1),
                BiddingEndDate = DateTime.UtcNow.AddDays(1),
                IsAutomaticSelectionEnabled = false
            };

            db.TransportRequests.Add(tr);
            db.SaveChanges();

            var bid = new Bid
            {
                TransportRequestId = tr.TransportRequestId,
                DriverId = driver.Id,
                Value = 80,
                DeliveryDeadline = DateTime.UtcNow.AddDays(2),
                Status = EBidStatus.Accepted
            };

            db.Bids.Add(bid);
            db.SaveChanges();

            tr.SelectedBidId = bid.BidId;
            db.SaveChanges();

            return (company, driver, tr, bid);
        }

        [Fact]
        public async Task ProcessPayment_Successfully_ConfirmsPayment_AndNotifies()
        {
            // Arrange
            var (controller, db, gateway, notifications) = BuildAsCompany(companyId: 10);
            var (company, driver, tr, bid) = SeedSelectedBid(db);
            controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("userId", company.Id.ToString()), new Claim("userType", "Company") }, "TestAuth"));
            gateway.Setup(g => g.ChargeAsync(It.IsAny<long>(), "eur", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IDictionary<string, string>>()))
                .ReturnsAsync(new ChargeResult(true, null));
            var dto = new CreatePaymentRequestDTO { TransportRequestId = tr.TransportRequestId, StripeToken = "tok_test" };

            // Act
            var result = await controller.ProcessPayment(dto);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            var payload = ok.Value as dynamic;

            Assert.Equal("Payment successfully processed.", (string)payload.message);
            var payment = await db.Payments.FirstAsync();

            Assert.Equal(EPaymentStatus.Confirmed, payment.PaymentStatus);
            Assert.Equal(ERequestStatus.WaitingPickup, tr.Status);
            Assert.Contains(notifications.Created, n => n.Type == ENotificationType.Confirmed_Payment && n.TransportRequestId == tr.TransportRequestId);
        }

        [Fact]
        public async Task ProcessPayment_Failed_ReturnsBadRequest()
        {
            // Arrange
            var (controller, db, gateway, notifications) = BuildAsCompany(companyId: 11);
            var (company, driver, tr, bid) = SeedSelectedBid(db);
            controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("userId", company.Id.ToString()), new Claim("userType", "Company") }, "TestAuth"));
            gateway.Setup(g => g.ChargeAsync(It.IsAny<long>(), "eur", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IDictionary<string, string>>()))
                .ReturnsAsync(new ChargeResult(false, "declined"));
            var dto = new CreatePaymentRequestDTO { TransportRequestId = tr.TransportRequestId, StripeToken = "tok_test" };

            // Act
            var result = await controller.ProcessPayment(dto);

            // Assert
            var bad = Assert.IsType<BadRequestObjectResult>(result);
            var payment = await db.Payments.FirstAsync();

            Assert.Equal(EPaymentStatus.Failed, payment.PaymentStatus);
        }

        [Fact]
        public async Task GetPaymentsByUser_ReturnsList()
        {
            // Arrange
            var (controller, db, gateway, notifications) = BuildAsCompany(companyId: 12);
            var (company, driver, tr, bid) = SeedSelectedBid(db);
            controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("userId", company.Id.ToString()), new Claim("userType", "Company") }, "TestAuth"));
            // seed a confirmed payment
            db.Payments.Add(new Payment { CompanyId = company.Id, DriverId = driver.Id, TransportRequestId = tr.TransportRequestId, GrossValue = bid.Value, Tax = Math.Round(bid.Value *0.05m,2), NetValue = bid.Value - Math.Round(bid.Value *0.05m,2), PaymentMethod = EPaymentMethod.Stripe, PaymentStatus = EPaymentStatus.Confirmed, CreatedAt = DateTime.UtcNow.AddHours(-1), CompletedAt = DateTime.UtcNow });
            await db.SaveChangesAsync();

            // Act
            var result = await controller.GetPaymentsByUser(company.Id);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            var list = Assert.IsType<List<PaymentResultDTO>>(ok.Value);
            Assert.Single(list);
        }

        [Fact]
        public async Task Retry_Succeeds_UpdatesState_AndNotifies()
        {
            // Arrange
            var (controller, db, gateway, notifications) = BuildAsCompany(companyId: 13);
            var (company, driver, tr, bid) = SeedSelectedBid(db);
            controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("userId", company.Id.ToString()), new Claim("userType", "Company") }, "TestAuth"));
            var failed = new Payment { CompanyId = company.Id, DriverId = driver.Id, TransportRequestId = tr.TransportRequestId, GrossValue = bid.Value, Tax = Math.Round(bid.Value *0.05m,2), NetValue = bid.Value - Math.Round(bid.Value *0.05m,2), PaymentMethod = EPaymentMethod.Stripe, PaymentStatus = EPaymentStatus.Failed, FailureReason = "declined", CreatedAt = DateTime.UtcNow.AddHours(-1) };
            db.Payments.Add(failed);
            await db.SaveChangesAsync();
            gateway.Setup(g => g.ChargeAsync(It.IsAny<long>(), "eur", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IDictionary<string, string>>()))
                .ReturnsAsync(new ChargeResult(true, null));

            // Act
            var result = await controller.Retry(failed.PaymentId, new RetryPaymentRequestDTO { StripeToken = "tok_retry" });

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            var fromDb = await db.Payments.FindAsync(failed.PaymentId);

            Assert.Equal(EPaymentStatus.Confirmed, fromDb!.PaymentStatus);
            Assert.Contains(notifications.Created, n => n.Type == ENotificationType.Confirmed_Payment && n.TransportRequestId == tr.TransportRequestId);
        }

        // simple in-file notification spy for assertions
        private sealed class TestNotificationService : INotificationService
        {
            public List<Notification> Created { get; } = new();

            public Task<List<Notification>> GetNotificationsAsync(int userId, ENotificationType? type = null, string order = "desc")
            {
                var q = Created.Where(n => n.UserId == userId);
                if (type.HasValue) q = q.Where(n => n.Type == type);
                return Task.FromResult(q.ToList());
            }

            public Task<Notification> CreateAndSendAsync(int userId, string context, ENotificationType type, int? bidId = null, int? transportRequestId = null)
            {
                var n = new Notification
                {
                    NotificationId = Created.Count + 1,
                    UserId = userId,
                    Context = context,
                    Type = type,
                    BidId = bidId,
                    TransportRequestId = transportRequestId,
                    TimeStamp = DateTime.UtcNow
                };

                Created.Add(n);
                return Task.FromResult(n);
            }

            public Task SendToMultipleUsersAsync(IEnumerable<int> userIds, string context, ENotificationType type)
            {
                foreach (var id in userIds)
                {
                    Created.Add(new Notification
                    {
                        NotificationId = Created.Count + 1,
                        UserId = id,
                        Context = context,
                        Type = type,
                        TimeStamp = DateTime.UtcNow
                    });
                }

                return Task.CompletedTask;
            }

            public Task MarkAsReadAsync(int notificationId)
            {
                throw new NotImplementedException();
            }

            public Task MarkAllAsReadAsync(int userId)
            {
                throw new NotImplementedException();
            }
        }
    }
}
