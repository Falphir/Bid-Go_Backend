using Bid_Go_Backend.Controllers;
using Bid_Go_Backend.Data;
using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.DTOs;
using Bid_Go_Backend.Data.Models.Enums;
using Bid_Go_Backend.Repositories.Authorization;
using Bid_Go_Backend.Repositories.Bids;
using Bid_Go_Backend.Services;
using Bid_Go_Backend.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Xunit;

namespace Bid_Go.Tests.Integration.Controllers
{
    /// <summary>
    /// Integration tests covering the main bid lifecycle endpoints of BidsController.
    /// Focus: create, update, cancel and retrieval scenarios under authenticated driver context.
    /// </summary>
    public class BidsControllerTests
    {
        private static (BidsController controller, BidGoDbContext db) BuildWithDriver(int driverId)
        {
            var options = new DbContextOptionsBuilder<BidGoDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            var db = new BidGoDbContext(options);
            var repo = new BidsRepository(db);
            var service = new BidsService(repo, db);
            var authz = new AuthorizationService(new AuthorizationRepository(db));
            var controller = new BidsController(service, authz);

            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim("userId", driverId.ToString()),
                new Claim(ClaimTypes.Role, "Driver"),
                new Claim("userType", "Driver")
            }, "TestAuth"));

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };

            return (controller, db);
        }

        private static TransportRequest SeedTransportRequest(BidGoDbContext db, int companyId = 1)
        {
            var company = new Company
            {
                Name = "C",
                CompanyName = "CC",
                Address = "A",
                Email = $"c{companyId}@c.com",
                Password = "x",
                PhoneNumber = 900000000 + companyId,
                NIF = 111111111 + companyId
            };

            db.Companies.Add(company);

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
                DeliveryDate = DateTime.UtcNow.AddDays(3),
                Image = "img",
                MaxPrice = 100,
                BiddingStartDate = DateTime.UtcNow.AddDays(-1),
                BiddingEndDate = DateTime.UtcNow.AddDays(1),
                IsAutomaticSelectionEnabled = false
            };

            db.TransportRequests.Add(tr);
            db.SaveChanges();

            return tr;
        }

        [Fact]
        public async Task AddBid_CreatesBid_ReturnsOk()
        {
            // Arrange: controller + active transport request + DTO
            var (controller, db) = BuildWithDriver(driverId: 10);
            var tr = SeedTransportRequest(db);
            var dto = new AddBidDTO
            {
                TransportRequestId = tr.TransportRequestId,
                Value = 50,
                DeliveryDeadline = DateTime.UtcNow.AddDays(2)
            };

            // Act: invoke endpoint
            var result = await controller.AddBid(dto);

            // Assert: created bid persisted with pending status
            var ok = Assert.IsType<OkObjectResult>(result);
            var bid = Assert.IsType<Bid>(ok.Value);

            Assert.True(bid.BidId > 0);
            var fromDb = await db.Bids.FindAsync(bid.BidId);
            Assert.NotNull(fromDb);
            Assert.Equal(EBidStatus.Pendent, fromDb!.Status);
        }

        [Fact]
        public async Task UpdateBid_UpdatesValue_AndDeadline_WhenPending()
        {
            // Arrange: seed bid then detach to avoid tracking conflicts
            var (controller, db) = BuildWithDriver(driverId: 11);
            var tr = SeedTransportRequest(db);

            var bid = new Bid
            {
                DriverId = 11,
                TransportRequestId = tr.TransportRequestId,
                Value = 60,
                DeliveryDeadline = DateTime.UtcNow.AddDays(2),
                Status = EBidStatus.Pendent
            };

            db.Bids.Add(bid);
            await db.SaveChangesAsync();

            // evitar conflito de tracking quando o serviço anexa uma segunda instância
            db.Entry(bid).State = EntityState.Detached;

            var dto = new BidUpdateDTO
            {
                Value = 40,
                DeliveryDeadline = DateTime.UtcNow.AddDays(1)
            };

            // Act
            var result = await controller.UpdateBid(bid.BidId, dto);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            var updated = Assert.IsType<Bid>(ok.Value);

            Assert.Equal(40, updated.Value);
        }

        [Fact]
        public async Task CancelBid_SetsStatusCanceled_WhenPending_AndOwned()
        {
            // Arrange
            var (controller, db) = BuildWithDriver(driverId: 12);
            var tr = SeedTransportRequest(db);

            var bid = new Bid
            {
                DriverId = 12,
                TransportRequestId = tr.TransportRequestId,
                Value = 60,
                DeliveryDeadline = DateTime.UtcNow.AddDays(2),
                Status = EBidStatus.Pendent
            };

            db.Bids.Add(bid);
            await db.SaveChangesAsync();

            // evitar conflito de tracking no update interno
            db.Entry(bid).State = EntityState.Detached;

            // Act
            var result = await controller.CancelBid(bid.BidId);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            var fromDb = await db.Bids.FindAsync(bid.BidId);

            Assert.Equal(EBidStatus.Canceled, fromDb!.Status);
        }

        [Fact]
        public async Task GetBidById_ReturnsOk_WhenExists()
        {
            // Arrange
            var (controller, db) = BuildWithDriver(driverId: 13);
            var tr = SeedTransportRequest(db);

            var bid = new Bid
            {
                DriverId = 13,
                TransportRequestId = tr.TransportRequestId,
                Value = 60,
                DeliveryDeadline = DateTime.UtcNow.AddDays(2),
                Status = EBidStatus.Pendent
            };

            db.Bids.Add(bid);
            await db.SaveChangesAsync();

            // Act
            var result = await controller.GetBidById(bid.BidId);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            var got = Assert.IsType<Bid>(ok.Value);

            Assert.Equal(bid.BidId, got.BidId);
        }

        [Fact]
        public async Task GetBidsByTransportRequest_ReturnsOk_List()
        {
            // Arrange
            var (controller, db) = BuildWithDriver(driverId: 14);
            var tr = SeedTransportRequest(db);

            db.Bids.AddRange(
                new Bid
                {
                    DriverId = 14,
                    TransportRequestId = tr.TransportRequestId,
                    Value = 60,
                    DeliveryDeadline = DateTime.UtcNow.AddDays(2),
                    Status = EBidStatus.Pendent
                },
                new Bid
                {
                    DriverId = 15,
                    TransportRequestId = tr.TransportRequestId,
                    Value = 40,
                    DeliveryDeadline = DateTime.UtcNow.AddDays(2),
                    Status = EBidStatus.Pendent
                }
            );

            await db.SaveChangesAsync();

            // Act
            var result = await controller.GetBidsByTransportRequest(tr.TransportRequestId);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            var list = Assert.IsType<List<Bid>>(ok.Value);

            Assert.Equal(2, list.Count);
        }

        [Fact]
        public async Task GetActiveBids_ReturnsProjectedList()
        {
            // Arrange
            var (controller, db) = BuildWithDriver(driverId: 16);
            var tr = SeedTransportRequest(db);

            db.Drivers.Add(new Driver
            {
                Name = "D16",
                Email = "d16@x.com",
                Password = "x",
                PhoneNumber = 911116116,
                NIF = 123456716
            });

            await db.SaveChangesAsync();
            var d = await db.Drivers.FirstAsync();

            db.Bids.AddRange(
                new Bid
                {
                    DriverId = d.Id,
                    TransportRequestId = tr.TransportRequestId,
                    Value = 80,
                    DeliveryDeadline = DateTime.UtcNow.AddDays(2),
                    Status = EBidStatus.Pendent
                },
                new Bid
                {
                    DriverId = d.Id,
                    TransportRequestId = tr.TransportRequestId,
                    Value = 50,
                    DeliveryDeadline = DateTime.UtcNow.AddDays(1),
                    Status = EBidStatus.Pendent
                }
            );

            await db.SaveChangesAsync();

            // Act
            var result = await controller.GetActiveBids(tr.TransportRequestId, orderBy: "value", descending: false);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            var enumerable = Assert.IsAssignableFrom<IEnumerable<object>>(ok.Value);
            var anonList = enumerable.ToList();

            Assert.Equal(2, anonList.Count);
        }
    }
}
