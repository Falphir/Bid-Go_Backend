using Bid_Go_Backend.Controllers;
using Bid_Go_Backend.Data;
using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.Enums;
using Bid_Go_Backend.Repositories.Authorization;
using Bid_Go_Backend.Repositories.Bids;
using Bid_Go_Backend.Services;
using Bid_Go_Backend.Services.Bids;
using Bid_Go_Backend.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Xunit;

namespace Bid_Go.Tests.Integration.Controllers
{
    public class AcceptAndRejectBidManualControllerTests
    {
        private static (AcceptAndRejectBidManualController controller, BidGoDbContext db, TestNotificationService notifications) BuildController(int companyId)
        {
            var options = new DbContextOptionsBuilder<BidGoDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            var db = new BidGoDbContext(options);

            var bidRepo = new AcceptAndRejectBidManualRepository(db);
            var notifications = new TestNotificationService();
            var service = new AcceptAndRejectBidManualService(bidRepo, notifications);

            var authzRepo = new AuthorizationRepository(db);
            var authzService = new AuthorizationService(authzRepo);

            var controller = new AcceptAndRejectBidManualController(service, authzService);

            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim("userId", companyId.ToString()),
                new Claim(ClaimTypes.Role, "Company")
            }, "TestAuth"));

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };

            return (controller, db, notifications);
        }

        private static (TransportRequest req, Bid b1, Bid b2, Bid b3) SeedRequestWithBids(BidGoDbContext db, int companyId)
        {
            var tr = new TransportRequest
            {
                CompanyId = companyId,
                Status = ERequestStatus.Active,
                Origin = "A",
                Destination = "B",
                Package = "Box",
                Weight = 1,
                Volume = 1,
                Length = 1,
                Width = 1,
                Height = 1,
                PickupDate = DateTime.UtcNow,
                DeliveryDate = DateTime.UtcNow.AddDays(1),
                Image = "img",
                MaxPrice = 100,
                BiddingStartDate = DateTime.UtcNow.AddDays(-1),
                BiddingEndDate = DateTime.UtcNow.AddDays(1),
                IsAutomaticSelectionEnabled = false
            };

            db.TransportRequests.Add(tr);
            db.SaveChanges();

            var b1 = new Bid { DriverId = 1, TransportRequestId = tr.TransportRequestId, Status = EBidStatus.Pendent, Value = 10, DeliveryDeadline = DateTime.UtcNow.AddDays(1) };
            var b2 = new Bid { DriverId = 2, TransportRequestId = tr.TransportRequestId, Status = EBidStatus.Pendent, Value = 12, DeliveryDeadline = DateTime.UtcNow.AddDays(1) };
            var b3 = new Bid { DriverId = 3, TransportRequestId = tr.TransportRequestId, Status = EBidStatus.Pendent, Value = 15, DeliveryDeadline = DateTime.UtcNow.AddDays(1) };

            db.Bids.AddRange(b1, b2, b3);
            db.SaveChanges();

            return (tr, b1, b2, b3);
        }

        [Fact]
        public async Task GetBidsByRequest_ReturnsOk_WithList()
        {
            var (controller, db, _) = BuildController(companyId: 10);
            var (tr, b1, b2, b3) = SeedRequestWithBids(db, 10);

            var result = await controller.GetBidsByTransportRequest(tr.TransportRequestId);
            var ok = Assert.IsType<OkObjectResult>(result);
            var list = Assert.IsType<List<Bid>>(ok.Value);

            Assert.Equal(3, list.Count);
        }

        [Fact]
        public async Task GetBidsByRequestAndStatus_ReturnsOk_WithFilteredList()
        {
            var (controller, db, _) = BuildController(companyId: 10);
            var (tr, b1, b2, b3) = SeedRequestWithBids(db, 10);

            // set one to Accepted to test filter
            var bidToAccept = db.Bids.First(b => b.BidId == b2.BidId);
            bidToAccept.Status = EBidStatus.Accepted;
            await db.SaveChangesAsync();

            var result = await controller.GetBidsByTransportRequestAndStatus(tr.TransportRequestId, EBidStatus.Accepted);
            var ok = Assert.IsType<OkObjectResult>(result);
            var list = Assert.IsType<List<Bid>>(ok.Value);

            Assert.Single(list);
            Assert.Equal(b2.BidId, list[0].BidId);
        }

        [Fact]
        public async Task AcceptBid_ChangesStatuses_SendsNotifications()
        {
            var (controller, db, notifications) = BuildController(companyId: 10);
            var (tr, target, other1, other2) = SeedRequestWithBids(db, 10);

            var result = await controller.AcceptBid(target.BidId);
            var ok = Assert.IsType<OkObjectResult>(result);

            Assert.Equal(200, ok.StatusCode);

            var req = await db.TransportRequests.FindAsync(tr.TransportRequestId);
            Assert.Equal(ERequestStatus.Pending, req!.Status);
            Assert.Equal(target.BidId, req.SelectedBidId);

            var bids = await db.Bids.Where(b => b.TransportRequestId == tr.TransportRequestId).ToListAsync();
            Assert.Contains(bids, b => b.BidId == target.BidId && b.Status == EBidStatus.Accepted);
            Assert.Equal(2, bids.Count(b => b.Status == EBidStatus.Rejected));

            Assert.Contains(notifications.Created, n => n.Type == ENotificationType.Accepted && n.BidId == target.BidId);
            Assert.Equal(2, notifications.Created.Count(n => n.Type == ENotificationType.Rejected && n.TransportRequestId == tr.TransportRequestId));
        }

        [Fact]
        public async Task RejectBid_ChangesStatus_SendsNotification()
        {
            var (controller, db, notifications) = BuildController(companyId: 10);
            var (tr, target, other1, other2) = SeedRequestWithBids(db, 10);

            var result = await controller.RejectBid(target.BidId);
            var ok = Assert.IsType<OkObjectResult>(result);

            Assert.Equal(200, ok.StatusCode);

            var b = await db.Bids.FindAsync(target.BidId);
            Assert.Equal(EBidStatus.Rejected, b!.Status);

            Assert.Contains(notifications.Created, n => n.Type == ENotificationType.Rejected && n.BidId == target.BidId);
        }

        // Test double inside the test file (no external utils)
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
        }
    }
}
