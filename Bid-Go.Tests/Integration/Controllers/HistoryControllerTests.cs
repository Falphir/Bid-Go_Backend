using Bid_Go_Backend.Controllers;
using Bid_Go_Backend.Data;
using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.Enums;
using Bid_Go_Backend.Data.Models.DTOs;
using Bid_Go_Backend.Repositories.History;
using Bid_Go_Backend.Services.History;
using Bid_Go_Backend.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using Xunit;

namespace Bid_Go.Tests.Integration.Controllers
{
    public class HistoryControllerTests
    {
        private static (HistoryController controller, BidGoDbContext db) BuildAs(string role, int userId)
        {
            var options = new DbContextOptionsBuilder<BidGoDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            var db = new BidGoDbContext(options);

            var repo = new HistoryRepository(db);
            var serviceLogger = LoggerFactory.Create(b => b.AddDebug()).CreateLogger<HistoryService>();
            var service = new HistoryService(repo, serviceLogger);
            var controllerLogger = LoggerFactory.Create(b => b.AddDebug()).CreateLogger<HistoryController>();
            var controller = new HistoryController(service, controllerLogger);

            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim("userId", userId.ToString()),
                new Claim("userType", role)
            }, "TestAuth"));

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };

            return (controller, db);
        }

        private static (Driver driver, Company company, TransportRequest tr1, TransportRequest tr2, Bid b1, Bid b2) SeedDriverHistory(BidGoDbContext db)
        {
            var company = new Company
            {
                Name = "Comp",
                CompanyName = "Comp Lda",
                Address = "Addr",
                Email = "c@x.com",
                Password = "x",
                PhoneNumber = 900000000,
                NIF = 111111111
            };

            var driver = new Driver
            {
                Name = "Driver X",
                Email = "d@x.com",
                Password = "x",
                PhoneNumber = 911111111,
                NIF = 222222222
            };

            db.Companies.Add(company);
            db.Drivers.Add(driver);
            db.SaveChanges();

            var tr1 = new TransportRequest
            {
                CompanyId = company.Id,
                Status = ERequestStatus.Completed,
                Origin = "O1",
                Destination = "D1",
                Package = "P1",
                Weight = 1,
                Volume = 1,
                Length = 1,
                Width = 1,
                Height = 1,
                PickupDate = DateTime.UtcNow.AddDays(-5),
                DeliveryDate = DateTime.UtcNow.AddDays(-2),
                Image = "img",
                MaxPrice = 100,
                BiddingStartDate = DateTime.UtcNow.AddDays(-10),
                BiddingEndDate = DateTime.UtcNow.AddDays(-6),
                IsAutomaticSelectionEnabled = false
            };

            var tr2 = new TransportRequest
            {
                CompanyId = company.Id,
                Status = ERequestStatus.Completed,
                Origin = "O2",
                Destination = "D2",
                Package = "P2",
                Weight = 2,
                Volume = 2,
                Length = 2,
                Width = 2,
                Height = 2,
                PickupDate = DateTime.UtcNow.AddDays(-4),
                DeliveryDate = DateTime.UtcNow.AddDays(-1),
                Image = "img",
                MaxPrice = 200,
                BiddingStartDate = DateTime.UtcNow.AddDays(-9),
                BiddingEndDate = DateTime.UtcNow.AddDays(-5),
                IsAutomaticSelectionEnabled = false
            };

            db.TransportRequests.AddRange(tr1, tr2);
            db.SaveChanges();

            var b1 = new Bid
            {
                TransportRequestId = tr1.TransportRequestId,
                DriverId = driver.Id,
                Value = 50,
                DeliveryDeadline = DateTime.UtcNow.AddDays(-3),
                Status = EBidStatus.Accepted
            };

            var b2 = new Bid
            {
                TransportRequestId = tr2.TransportRequestId,
                DriverId = driver.Id,
                Value = 150,
                DeliveryDeadline = DateTime.UtcNow.AddDays(-1),
                Status = EBidStatus.Rejected
            };

            db.Bids.AddRange(b1, b2);
            db.SaveChanges();

            // add one review for tr1 to populate rating
            db.Reviews.Add(new ReviewCompany
            {
                CompanyId = company.Id,
                DriverId = driver.Id,
                TransportRequestId = tr1.TransportRequestId,
                Classification = 4.5m,
                TimeStamp = DateTime.UtcNow
            });

            db.SaveChanges();

            return (driver, company, tr1, tr2, b1, b2);
        }

        private static (Company company, Driver driver, TransportRequest tr1, TransportRequest tr2, Bid accepted) SeedCompanyHistory(BidGoDbContext db)
        {
            var company = new Company
            {
                Name = "Comp2",
                CompanyName = "Comp2 Lda",
                Address = "Addr2",
                Email = "c2@x.com",
                Password = "x",
                PhoneNumber = 900000001,
                NIF = 111111112
            };

            var driver = new Driver
            {
                Name = "D2",
                Email = "d2@x.com",
                Password = "x",
                PhoneNumber = 922222222,
                NIF = 333333333
            };

            db.Companies.Add(company);
            db.Drivers.Add(driver);
            db.SaveChanges();

            var tr1 = new TransportRequest
            {
                CompanyId = company.Id,
                Status = ERequestStatus.Completed,
                Origin = "O1",
                Destination = "D1",
                Package = "P1",
                Weight = 1,
                Volume = 1,
                Length = 1,
                Width = 1,
                Height = 1,
                PickupDate = DateTime.UtcNow.AddDays(-5),
                DeliveryDate = DateTime.UtcNow.AddDays(-2),
                Image = "img",
                MaxPrice = 100,
                BiddingStartDate = DateTime.UtcNow.AddDays(-10),
                BiddingEndDate = DateTime.UtcNow.AddDays(-6),
                IsAutomaticSelectionEnabled = false
            };

            var tr2 = new TransportRequest
            {
                CompanyId = company.Id,
                Status = ERequestStatus.Completed,
                Origin = "O2",
                Destination = "D2",
                Package = "P2",
                Weight = 2,
                Volume = 2,
                Length = 2,
                Width = 2,
                Height = 2,
                PickupDate = DateTime.UtcNow.AddDays(-4),
                DeliveryDate = DateTime.UtcNow.AddDays(-1),
                Image = "img",
                MaxPrice = 200,
                BiddingStartDate = DateTime.UtcNow.AddDays(-9),
                BiddingEndDate = DateTime.UtcNow.AddDays(-5),
                IsAutomaticSelectionEnabled = false
            };

            db.TransportRequests.AddRange(tr1, tr2);
            db.SaveChanges();

            var accepted = new Bid
            {
                TransportRequestId = tr1.TransportRequestId,
                DriverId = driver.Id,
                Value = 70,
                DeliveryDeadline = DateTime.UtcNow.AddDays(-3),
                Status = EBidStatus.Accepted
            };

            db.Bids.Add(accepted);
            db.SaveChanges();

            return (company, driver, tr1, tr2, accepted);
        }

        [Fact]
        public async Task GetDriverHistory_ReturnsOk_WithItems()
        {
            var (controller, db) = BuildAs("Driver", userId: 10);
            var (driver, company, tr1, tr2, b1, b2) = SeedDriverHistory(db);

            // impersonate correct driver id
            controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim("userId", driver.Id.ToString()),
                new Claim("userType", "Driver")
            }, "TestAuth"));

            var result = await controller.GetDriverHistory(driver.Id);
            var ok = Assert.IsType<OkObjectResult>(result);
            var items = Assert.IsType<List<BidHistoryDTO>>(ok.Value);

            Assert.Equal(2, items.Count);
            Assert.Contains(items, i => i.Value == b1.Value && i.Status == b1.Status);
            Assert.Contains(items, i => i.Value == b2.Value && i.Status == b2.Status);
            Assert.Contains(items, i => i.Rating.HasValue);
        }

        [Fact]
        public async Task GetDriverHistory_ReturnsNotFound_WhenNoHistory()
        {
            var (controller, db) = BuildAs("Driver", userId: 1);
            var driver = new Driver { Name = "NoHist", Email = "n@x.com", Password = "x", PhoneNumber = 933333333, NIF = 444444444 };
            db.Drivers.Add(driver);
            await db.SaveChangesAsync();

            controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim("userId", driver.Id.ToString()),
                new Claim("userType", "Driver")
            }, "TestAuth"));

            var result = await controller.GetDriverHistory(driver.Id);
            var notFound = Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task GetDriverHistory_ReturnsForbid_WhenUserMismatch()
        {
            var (controller, db) = BuildAs("Driver", userId: 999);
            var result = await controller.GetDriverHistory(5);
            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task GetCompanyHistory_ReturnsOk_WithItems()
        {
            var (controller, db) = BuildAs("Company", userId: 20);
            var (company, driver, tr1, tr2, accepted) = SeedCompanyHistory(db);

            controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim("userId", company.Id.ToString()),
                new Claim("userType", "Company")
            }, "TestAuth"));

            var result = await controller.GetCompanyHistory(company.Id);
            var ok = Assert.IsType<OkObjectResult>(result);
            var items = Assert.IsType<List<TransportHistoryDTO>>(ok.Value);

            Assert.Equal(2, items.Count);
            Assert.Contains(items, i => i.TransportRequestId == tr1.TransportRequestId && i.Price == accepted.Value);
            Assert.Contains(items, i => i.TransportRequestId == tr2.TransportRequestId && i.Price == 0);
        }

        [Fact]
        public async Task GetCompanyHistory_ReturnsNotFound_WhenNoHistory()
        {
            var (controller, db) = BuildAs("Company", userId: 2);
            var company = new Company { Name = "NoHistCo", CompanyName = "NH", Address = "A", Email = "nh@x.com", Password = "x", PhoneNumber = 955555555, NIF = 555555555 };
            db.Companies.Add(company);
            await db.SaveChangesAsync();

            controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim("userId", company.Id.ToString()),
                new Claim("userType", "Company")
            }, "TestAuth"));

            var result = await controller.GetCompanyHistory(company.Id);
            var notFound = Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task GetCompanyHistory_ReturnsForbid_WhenUserMismatch()
        {
            var (controller, db) = BuildAs("Company", userId: 999);
            var result = await controller.GetCompanyHistory(5);
            Assert.IsType<ForbidResult>(result);
        }
    }
}
