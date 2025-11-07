using Bid_Go_Backend.Controllers;
using Bid_Go_Backend.Data;
using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.Enums;
using Bid_Go_Backend.Repositories.Interfaces;
using Bid_Go_Backend.Services.Bids;
using Bid_Go_Backend.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Xunit;

namespace Bid_Go.Tests.Integration.Controllers
{
 /// <summary>
 /// Integration tests for automatic bid selection endpoint: success and error paths.
 /// </summary>
 public class AutomaticSelectionAlgorithmControllerTests
 {
        private static (AutomaticSelectionAlgorithmController controller, BidGoDbContext db, TestNotificationService notifications) Build()
        {
            var options = new DbContextOptionsBuilder<BidGoDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            var db = new BidGoDbContext(options);
            var repo = new TestAutoRepo(db);
            var notifications = new TestNotificationService();
            var service = new AutomaticSelectionAlgorithmService(repo, notifications);
            var controller = new AutomaticSelectionAlgorithmController(service);

            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim("userId", "10"),
                new Claim(ClaimTypes.Role, "Company")
            }, "TestAuth"));

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };

            return (controller, db, notifications);
        }

        private static (TransportRequest tr, Driver d1, Driver d2, Driver d3, Bid b1, Bid b2, Bid b3) SeedScenario(BidGoDbContext db)
        {
            var company = new Company
            {
                Name = "C",
                CompanyName = "CC",
                Address = "A",
                Email = "c@c.com",
                Password = "x",
                PhoneNumber = 900000000,
                NIF = 111111111
            };
            db.Companies.Add(company);

            var d1 = new Driver { Name = "D1", Email = "d1@x.com", Password = "x", PhoneNumber = 911111111, NIF = 123456700 };
            var d2 = new Driver { Name = "D2", Email = "d2@x.com", Password = "x", PhoneNumber = 922222222, NIF = 123456701 };
            var d3 = new Driver { Name = "D3", Email = "d3@x.com", Password = "x", PhoneNumber = 933333333, NIF = 123456702 };
            db.Drivers.AddRange(d1, d2, d3);

            var tr = new TransportRequest
            {
                Company = company,
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
                BiddingStartDate = DateTime.UtcNow.AddDays(-2),
                BiddingEndDate = DateTime.UtcNow.AddDays(-1),
                IsAutomaticSelectionEnabled = true
            };
            db.TransportRequests.Add(tr);
            db.SaveChanges();

            // reputations: d1=4.5, d2=3.0, d3=2.0
            db.Reviews.AddRange(
                new ReviewDriver { DriverId = d1.Id, CompanyId = company.Id, TransportRequestId = tr.TransportRequestId, Classification = 5 },
                new ReviewDriver { DriverId = d1.Id, CompanyId = company.Id, TransportRequestId = tr.TransportRequestId, Classification = 4 },
                new ReviewDriver { DriverId = d2.Id, CompanyId = company.Id, TransportRequestId = tr.TransportRequestId, Classification = 3 },
                new ReviewDriver { DriverId = d3.Id, CompanyId = company.Id, TransportRequestId = tr.TransportRequestId, Classification = 2 }
            );
            db.SaveChanges();

            var b1 = new Bid { TransportRequestId = tr.TransportRequestId, DriverId = d1.Id, Status = EBidStatus.Pendent, Value = 12, DeliveryDeadline = DateTime.UtcNow.AddDays(1) };
            var b2 = new Bid { TransportRequestId = tr.TransportRequestId, DriverId = d2.Id, Status = EBidStatus.Pendent, Value = 10, DeliveryDeadline = DateTime.UtcNow.AddDays(1) };
            var b3 = new Bid { TransportRequestId = tr.TransportRequestId, DriverId = d3.Id, Status = EBidStatus.Pendent, Value = 9, DeliveryDeadline = DateTime.UtcNow.AddDays(1) };

            db.Bids.AddRange(b1, b2, b3);
            db.SaveChanges();

            return (tr, d1, d2, d3, b1, b2, b3);
        }

        [Fact]
        public async Task ExecuteAlgorithm_SelectsBestBid_UpdatesState_AndNotifies()
        {
            // Arrange
            var (controller, db, notifications) = Build();
            var (tr, d1, d2, d3, b1, b2, b3) = SeedScenario(db);

            // Act
            var result = await controller.ExecuteAlgorithm(tr.TransportRequestId);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, ok.StatusCode);

            var trReload = await db.TransportRequests.FindAsync(tr.TransportRequestId);
            Assert.Equal(ERequestStatus.Pending, trReload!.Status);
            Assert.Equal(b2.BidId, trReload.SelectedBidId); // d2 selected

            var bids = await db.Bids.Where(b => b.TransportRequestId == tr.TransportRequestId).ToListAsync();
            Assert.Contains(bids, b => b.BidId == b2.BidId && b.Status == EBidStatus.Accepted);
            Assert.Equal(2, bids.Count(b => b.Status == EBidStatus.Rejected));

            Assert.Contains(notifications.Created, n => n.Type == ENotificationType.Accepted && n.BidId == b2.BidId);
            Assert.Equal(2, notifications.Created.Count(n => n.Type == ENotificationType.Rejected && n.TransportRequestId == tr.TransportRequestId));
        }

        [Fact]
        public async Task ExecuteAlgorithm_ReturnsBadRequest_WhenNotEnabled()
        {
            // Arrange
            var (controller, db, _) = Build();
            var (tr, d1, d2, d3, b1, b2, b3) = SeedScenario(db);

            tr.IsAutomaticSelectionEnabled = false;
            await db.SaveChangesAsync();

            // Act
            var result = await controller.ExecuteAlgorithm(tr.TransportRequestId);

            // Assert
            var bad = Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task ExecuteAlgorithm_ReturnsBadRequest_WhenBiddingNotFinished()
        {
            // Arrange
            var (controller, db, _) = Build();
            var (tr, d1, d2, d3, b1, b2, b3) = SeedScenario(db);

            tr.BiddingEndDate = DateTime.UtcNow.AddHours(1);
            await db.SaveChangesAsync();

            // Act
            var result = await controller.ExecuteAlgorithm(tr.TransportRequestId);

            // Assert
            var bad = Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task ExecuteAlgorithm_ReturnsBadRequest_WhenNoEligibleBids()
        {
            // Arrange
            var (controller, db, _) = Build();
            var (tr, d1, d2, d3, b1, b2, b3) = SeedScenario(db);

            // remove reviews to drop reputations below threshold
            db.Reviews.RemoveRange(db.Reviews);
            await db.SaveChangesAsync();

            // Act
            var result = await controller.ExecuteAlgorithm(tr.TransportRequestId);

            // Assert
            var bad = Assert.IsType<BadRequestObjectResult>(result);
        }

        // in-file repo using EF InMemory to satisfy interface
        private sealed class TestAutoRepo : IAutomaticSelectionAlgorithmRepository
        {
            private readonly BidGoDbContext _ctx;

            public TestAutoRepo(BidGoDbContext ctx) => _ctx = ctx;

            public Task<TransportRequest?> GetTransportRequestWithBidsAsync(int transportRequestId)
                => _ctx.TransportRequests
                    .Include(tr => tr.Bids)
                    .ThenInclude(b => b.Driver)
                    .FirstOrDefaultAsync(tr => tr.TransportRequestId == transportRequestId);

            public Task<Dictionary<int, decimal>> GetDriverReputationsAsync(IEnumerable<int> driverIds)
                => _ctx.Reviews
                    .Where(r => driverIds.Contains(r.DriverId))
                    .GroupBy(r => r.DriverId)
                    .Select(g => new { g.Key, Average = g.Average(r => r.Classification) })
                    .ToDictionaryAsync(x => x.Key, x => x.Average);

            public Task SaveChangesAsync() => _ctx.SaveChangesAsync();
        }

        // in-file notifications spy
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
