using Bid_Go_Backend.Controllers;
using Bid_Go_Backend.Data;
using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.DTOs;
using Bid_Go_Backend.Data.Models.Enums;
using Bid_Go_Backend.Repositories.Review;
using Bid_Go_Backend.Services.Review;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Bid_Go.Tests.Integration.Controllers
{
    /// <summary>
    /// Integration tests for review submission and retrieval endpoints.
    /// </summary>
    public class ReviewRequestControllerTests
    {
        private static (ReviewRequestController controller, BidGoDbContext db) Build()
        {
            var options = new DbContextOptionsBuilder<BidGoDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            var db = new BidGoDbContext(options);
            var repo = new ReviewRequestRepository(db, LoggerFactory.Create(b => b.AddDebug()).CreateLogger<ReviewRequestRepository>());
            var service = new ReviewRequestService(repo, LoggerFactory.Create(b => b.AddDebug()).CreateLogger<ReviewRequestService>());
            var controller = new ReviewRequestController(service);

            return (controller, db);
        }

        private static (Company company, Driver driver, TransportRequest tr) SeedCompletedRequest(BidGoDbContext db)
        {
            var company = new Company
            {
                Name = "C",
                CompanyName = "CC",
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
                Status = ERequestStatus.Completed,
                Origin = "O",
                Destination = "D",
                Package = "P",
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
                BiddingEndDate = DateTime.UtcNow.AddDays(-6)
            };

            db.TransportRequests.Add(tr);
            db.SaveChanges();

            return (company, driver, tr);
        }

        [Fact]
        public async Task SubmitReview_Company_Succeeds_Then_Driver_Fails_Duplicate()
        {
            // Arrange
            var (controller, db) = Build();
            var (company, driver, tr) = SeedCompletedRequest(db);
            var dtoCompany = new ReviewRequestServiceDTO { TimeStamp = DateTime.UtcNow, Classification =4.5m, DriverId = driver.Id, CompanyId = company.Id, TransportRequestId = tr.TransportRequestId, Discriminator = "Company", ServiceQuality =5, ClientSuport =4 };

            // Act + Assert first (success)
            var result1 = await controller.SubmitReview(dtoCompany);
            var ok = Assert.IsType<OkObjectResult>(result1);

            // Act duplicate
            var resultDup = await controller.SubmitReview(dtoCompany);

            // Assert duplicate conflict
            var bad = Assert.IsType<BadRequestObjectResult>(resultDup);
        }

        [Fact]
        public async Task SubmitReview_Driver_Succeeds()
        {
            var (controller, db) = Build();
            var (company, driver, tr) = SeedCompletedRequest(db);

            var dto = new ReviewRequestServiceDTO
            {
                TimeStamp = DateTime.UtcNow,
                Classification = 4.0m,
                DriverId = driver.Id,
                CompanyId = company.Id,
                TransportRequestId = tr.TransportRequestId,
                Discriminator = "Driver",
                Punctuality = 5,
                Behavior = 4
            };

            var result = await controller.SubmitReview(dto);
            var ok = Assert.IsType<OkObjectResult>(result);

            var fromDb = await db.Reviews.OfType<ReviewDriver>()
                .FirstOrDefaultAsync(r => r.TransportRequestId == tr.TransportRequestId && r.DriverId == driver.Id);

            Assert.NotNull(fromDb);
            Assert.Equal(4.0m, fromDb!.Classification);
        }

        [Fact]
        public async Task GetReviewsByService_ReturnsItems_FromBothTypes()
        {
            var (controller, db) = Build();
            var (company, driver, tr) = SeedCompletedRequest(db);

            db.Reviews.Add(new ReviewCompany
            {
                TransportRequestId = tr.TransportRequestId,
                CompanyId = company.Id,
                DriverId = driver.Id,
                Classification = 4.5m,
                ServiceQuality = 5,
                ClientSuport = 4,
                TimeStamp = DateTime.UtcNow
            });

            db.Reviews.Add(new ReviewDriver
            {
                TransportRequestId = tr.TransportRequestId,
                CompanyId = company.Id,
                DriverId = driver.Id,
                Classification = 4.0m,
                Punctuality = 5,
                Behavior = 4,
                TimeStamp = DateTime.UtcNow
            });

            await db.SaveChangesAsync();

            var result = await controller.GetReviewsByService(tr.TransportRequestId);
            var ok = Assert.IsType<OkObjectResult>(result);
            var items = Assert.IsAssignableFrom<IEnumerable<ReviewByServiceDTO>>(ok.Value);

            Assert.Equal(2, items.Count());
        }
    }
}
