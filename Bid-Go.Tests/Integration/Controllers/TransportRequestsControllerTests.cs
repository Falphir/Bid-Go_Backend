using Bid_Go_Backend.Controllers;
using Bid_Go_Backend.Data;
using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.DTOs;
using Bid_Go_Backend.Data.Models.Enums;
using Bid_Go_Backend.Repositories.Authorization;
using Bid_Go_Backend.Repositories.Transport_Request;
using Bid_Go_Backend.Services.Interfaces;
using Bid_Go_Backend.Services.Transport_Request;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text;
using Xunit;
using Bid_Go_Backend.Services;

namespace Bid_Go.Tests.Integration.Controllers
{
    public class TransportRequestsControllerTests
    {
        private static (TransportRequestsController controller, BidGoDbContext db) BuildAsCompany(int companyId)
        {
            var options = new DbContextOptionsBuilder<BidGoDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            var db = new BidGoDbContext(options);
            var repo = new TransportRequestRepository(db);
            var r2 = new TestR2();
            var service = new TransportRequestService(repo, r2);
            var authz = new AuthorizationService(new AuthorizationRepository(db));
            var controller = new TransportRequestsController(service, authz);

            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim("userId", companyId.ToString()),
                new Claim(ClaimTypes.Role, "Company")
            }, "TestAuth"));

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };

            return (controller, db);
        }

        private static IFormFile MakeFile(string name = "img.jpg")
        {
            var bytes = Encoding.UTF8.GetBytes("x");
            return new FormFile(new MemoryStream(bytes), 0, bytes.Length, name, name);
        }

        [Fact]
        public async Task Create_ReturnsCreated_AndPersists()
        {
            var (controller, db) = BuildAsCompany(companyId: 1);

            var dto = new CreateTransportRequestDTO
            {
                Origin = "O",
                Destination = "D",
                Package = "P",
                Weight = 1,
                Volume = 1,
                Length = 1,
                Width = 1,
                Height = 1,
                BiddingStartDate = DateTime.UtcNow.AddDays(-2),
                BiddingEndDate = DateTime.UtcNow.AddDays(-1),
                PickupDate = DateTime.UtcNow,
                DeliveryDate = DateTime.UtcNow.AddDays(2),
                IsAutomaticSelectionEnabled = false,
                MaxPrice = 50,
                CompanyId = 1
            };

            var image = MakeFile("r.png");
            var result = await controller.Create(dto, image);
            var created = Assert.IsType<CreatedAtActionResult>(result);
            var tr = Assert.IsType<TransportRequest>(created.Value);

            Assert.True(tr.TransportRequestId > 0);
            var fromDb = await db.TransportRequests.FindAsync(tr.TransportRequestId);

            Assert.NotNull(fromDb);
            Assert.Equal(ERequestStatus.Draft, fromDb!.Status);
            Assert.Equal("https://r2/r.png", fromDb.Image);
        }

        [Fact]
        public async Task Update_UpdatesDraft_WithNewImage()
        {
            var (controller, db) = BuildAsCompany(companyId: 2);

            var tr = new TransportRequest
            {
                CompanyId = 2,
                Status = ERequestStatus.Draft,
                Origin = "O",
                Destination = "D",
                Package = "P",
                Weight = 1,
                Volume = 1,
                Length = 1,
                Width = 1,
                Height = 1,
                PickupDate = DateTime.UtcNow.AddDays(3),
                DeliveryDate = DateTime.UtcNow.AddDays(5),
                BiddingStartDate = DateTime.UtcNow,
                BiddingEndDate = DateTime.UtcNow.AddDays(1),
                Image = "prev.jpg",
                MaxPrice = 50
            };

            db.TransportRequests.Add(tr);
            await db.SaveChangesAsync();

            var dto = new UpdateTransportRequestDTO
            {
                Origin = "O2",
                Destination = "D2",
                Package = "P2",
                MaxPrice = 60
            };

            var image = MakeFile("new.png");
            var result = await controller.Update(tr.TransportRequestId, dto, image);
            var ok = Assert.IsType<OkObjectResult>(result);
            var updated = Assert.IsType<TransportRequest>(ok.Value);

            Assert.Equal("O2", updated.Origin);
            Assert.Equal("D2", updated.Destination);
            Assert.Equal("P2", updated.Package);
            Assert.Equal(60, updated.MaxPrice);
            Assert.Equal("https://r2/new.png", updated.Image);
        }

        [Fact]
        public async Task Delete_ActiveRequest_ReturnsOk_AndRemoves()
        {
            var (controller, db) = BuildAsCompany(companyId: 3);

            var tr = new TransportRequest
            {
                CompanyId = 3,
                Status = ERequestStatus.Active,
                Origin = "O",
                Destination = "D",
                Package = "P",
                Weight = 1,
                Volume = 1,
                Length = 1,
                Width = 1,
                Height = 1,
                PickupDate = DateTime.UtcNow.AddDays(3),
                DeliveryDate = DateTime.UtcNow.AddDays(5),
                BiddingStartDate = DateTime.UtcNow.AddDays(-1),
                BiddingEndDate = DateTime.UtcNow,
                Image = "x.png",
                MaxPrice = 50
            };

            db.TransportRequests.Add(tr);
            await db.SaveChangesAsync();

            var result = await controller.Delete(tr.TransportRequestId);
            var ok = Assert.IsType<OkObjectResult>(result);
            var still = await db.TransportRequests.FindAsync(tr.TransportRequestId);

            Assert.Null(still);
        }

        [Fact]
        public async Task GetById_ReturnsOk_WhenExists()
        {
            var (controller, db) = BuildAsCompany(companyId: 4);

            var tr = new TransportRequest
            {
                CompanyId = 4,
                Status = ERequestStatus.Draft,
                Origin = "O",
                Destination = "D",
                Package = "P",
                Weight = 1,
                Volume = 1,
                Length = 1,
                Width = 1,
                Height = 1,
                PickupDate = DateTime.UtcNow.AddDays(3),
                DeliveryDate = DateTime.UtcNow.AddDays(5),
                BiddingStartDate = DateTime.UtcNow,
                BiddingEndDate = DateTime.UtcNow.AddDays(1),
                Image = "x.png",
                MaxPrice = 50
            };

            db.TransportRequests.Add(tr);
            await db.SaveChangesAsync();

            var result = await controller.GetById(tr.TransportRequestId);
            var ok = Assert.IsType<OkObjectResult>(result);
            var body = Assert.IsType<TransportRequest>(ok.Value);

            Assert.Equal(tr.TransportRequestId, body.TransportRequestId);
        }

        [Fact]
        public async Task GetByCompany_ReturnsList()
        {
            var (controller, db) = BuildAsCompany(companyId: 5);

            db.TransportRequests.AddRange(
                new TransportRequest
                {
                    CompanyId = 5,
                    Status = ERequestStatus.Draft,
                    Origin = "O",
                    Destination = "D",
                    Package = "P",
                    Weight = 1,
                    Volume = 1,
                    Length = 1,
                    Width = 1,
                    Height = 1,
                    PickupDate = DateTime.UtcNow.AddDays(3),
                    DeliveryDate = DateTime.UtcNow.AddDays(5),
                    BiddingStartDate = DateTime.UtcNow,
                    BiddingEndDate = DateTime.UtcNow.AddDays(1),
                    Image = "x.png",
                    MaxPrice = 50
                },
                new TransportRequest
                {
                    CompanyId = 5,
                    Status = ERequestStatus.Active,
                    Origin = "O2",
                    Destination = "D2",
                    Package = "P2",
                    Weight = 2,
                    Volume = 2,
                    Length = 2,
                    Width = 2,
                    Height = 2,
                    PickupDate = DateTime.UtcNow.AddDays(4),
                    DeliveryDate = DateTime.UtcNow.AddDays(6),
                    BiddingStartDate = DateTime.UtcNow,
                    BiddingEndDate = DateTime.UtcNow.AddDays(2),
                    Image = "y.png",
                    MaxPrice = 100
                }
            );

            await db.SaveChangesAsync();

            var result = await controller.GetByCompany(5);
            var ok = Assert.IsType<OkObjectResult>(result);
            var list = Assert.IsType<List<TransportRequest>>(ok.Value);

            Assert.Equal(2, list.Count);
        }

        private sealed class TestR2 : ICloudflareR2Service
        {
            public Task DeleteImageAsync(string fileName) => Task.CompletedTask;

            public Task<string> UploadImageAsync(IFormFile file) => Task.FromResult($"https://r2/{file.FileName}");
        }
    }
}
