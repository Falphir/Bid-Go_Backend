using Bid_Go_Backend.Controllers;
using Bid_Go_Backend.Data;
using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.DTOs;
using Bid_Go_Backend.Data.Models.Enums;
using Bid_Go_Backend.Repositories.Transport_Request;
using Bid_Go_Backend.Services.Transport_Request;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Bid_Go.Tests.Integration
{
    /// <summary>
    /// Integration tests for public transport requests page endpoints (filters and single view).
    /// </summary>
    public class TransportRequestsPageControllerTests
    {
        private static (TransportRequestsPageController controller, BidGoDbContext db) Build()
        {
            var options = new DbContextOptionsBuilder<BidGoDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            var db = new BidGoDbContext(options);
            var repo = new TransportRequestsPageRepository(db);
            var logger = LoggerFactory.Create(b => b.AddDebug()).CreateLogger<TransportRequestsPageService>();
            var service = new TransportRequestsPageService(repo, logger);
            var controller = new TransportRequestsPageController(service);

            return (controller, db);
        }

        [Fact]
        public async Task GetActive_ReturnsList_FilteredAndOrdered()
        {
            // Arrange
            var (controller, db) = Build();

            db.TransportRequests.AddRange(
                new TransportRequest
                {
                    Origin = "Lisboa",
                    Destination = "Porto",
                    Package = "P1",
                    PickupDate = DateTime.UtcNow.AddDays(1),
                    DeliveryDate = DateTime.UtcNow.AddDays(2),
                    Image = "i1",
                    MaxPrice = 100,
                    Status = ERequestStatus.Active
                },
                new TransportRequest
                {
                    Origin = "Lisboa",
                    Destination = "Faro",
                    Package = "P2",
                    PickupDate = DateTime.UtcNow.AddDays(1),
                    DeliveryDate = DateTime.UtcNow.AddDays(2),
                    Image = "i2",
                    MaxPrice = 50,
                    Status = ERequestStatus.Active
                },
                new TransportRequest
                {
                    Origin = "Braga",
                    Destination = "Porto",
                    Package = "P3",
                    PickupDate = DateTime.UtcNow.AddDays(1),
                    DeliveryDate = DateTime.UtcNow.AddDays(2),
                    Image = "i3",
                    MaxPrice = 75,
                    Status = ERequestStatus.Active
                }
            );

            await db.SaveChangesAsync();

            // Act
            var result = await controller.GetActive(origin: "Lisboa", destination: null, deliveryDate: null, priceOrder: "asc");

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var list = Assert.IsAssignableFrom<IEnumerable<object>>(ok.Value).ToList();
            Assert.Equal(2, list.Count);
        }

        [Fact]
        public async Task GetById_ReturnsOk_WhenActiveRequestExists()
        {
            var (controller, db) = Build();

            var tr = new TransportRequest
            {
                Origin = "A",
                Destination = "B",
                Package = "P",
                PickupDate = DateTime.UtcNow.AddDays(1),
                DeliveryDate = DateTime.UtcNow.AddDays(2),
                Image = "img",
                MaxPrice = 120,
                Status = ERequestStatus.Active,
                Weight = 1,
                Volume = 1,
                Length = 1,
                Width = 1,
                Height = 1
            };

            db.TransportRequests.Add(tr);
            await db.SaveChangesAsync();

            var result = await controller.GetById(tr.TransportRequestId);
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var dto = Assert.IsType<TransportRequestResponseDTO>(ok.Value);

            Assert.Equal(tr.MaxPrice, dto.MaxPrice);
        }
    }
}
