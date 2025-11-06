using Bid_Go_Backend.Data;
using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.DTOs;
using Bid_Go_Backend.Data.Models.Enums;
using Bid_Go_Backend.Repositories.Interfaces;
using Bid_Go_Backend.Services;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Bid_Go.Tests.Services
{
    public class BidsServiceTests
    {
        private readonly Mock<IBidsRepository> _mockRepo;
        private readonly BidGoDbContext _ctx;
        private readonly BidsService _service;

        public BidsServiceTests()
        {
            _mockRepo = new Mock<IBidsRepository>();

            var options = new DbContextOptionsBuilder<BidGoDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _ctx = new BidGoDbContext(options);
            _service = new BidsService(_mockRepo.Object, _ctx);
        }

        [Fact]
        public async Task AddBidAsync_Should_Fail_When_TransportRequest_NotFound()
        {
            var dto = new AddBidDTO
            {
                TransportRequestId = 1,
                Value = 100,
                DeliveryDeadline = DateTime.Now.AddDays(1)
            };

            var result = await _service.AddBidAsync(1, dto);

            Assert.False(result.Success);
            Assert.Equal("Transport request not found.", result.Message);
        }

        [Fact]
        public async Task AddBidAsync_Should_Fail_When_Request_Not_Active()
        {
            _ctx.TransportRequests.Add(new TransportRequest
            {
                TransportRequestId = 1,
                Status = ERequestStatus.Pending
            });
            await _ctx.SaveChangesAsync();

            var dto = new AddBidDTO
            {
                TransportRequestId = 1,
                Value = 50,
                DeliveryDeadline = DateTime.Now.AddDays(3)
            };

            var result = await _service.AddBidAsync(1, dto);

            Assert.False(result.Success);
            Assert.Equal("Cannot place a bid on a transport request that is not open.", result.Message);
        }

        [Fact]
        public async Task AddBidAsync_Should_Fail_When_Value_Invalid()
        {
            _ctx.TransportRequests.Add(new TransportRequest
            {
                TransportRequestId = 2,
                Status = ERequestStatus.Active,
                MaxPrice = 100,
                PickupDate = DateTime.Now,
                DeliveryDate = DateTime.Now.AddDays(5)
            });
            await _ctx.SaveChangesAsync();

            var dto = new AddBidDTO
            {
                TransportRequestId = 2,
                Value = -5,
                DeliveryDeadline = DateTime.Now.AddDays(3)
            };

            var result = await _service.AddBidAsync(1, dto);

            Assert.False(result.Success);
            Assert.Equal("Bid value must be greater than zero.", result.Message);
        }

        [Fact]
        public async Task AddBidAsync_Should_Fail_When_Deadline_Invalid()
        {
            var request = new TransportRequest
            {
                TransportRequestId = 3,
                Status = ERequestStatus.Active,
                MaxPrice = 100,
                PickupDate = DateTime.Now,
                DeliveryDate = DateTime.Now.AddDays(5)
            };
            _ctx.TransportRequests.Add(request);
            await _ctx.SaveChangesAsync();

            var dto = new AddBidDTO
            {
                TransportRequestId = 3,
                Value = 50,
                DeliveryDeadline = request.PickupDate.AddHours(-1)
            };

            var result = await _service.AddBidAsync(1, dto);

            Assert.False(result.Success);
            Assert.Equal("The bid's delivery deadline must be later than the pickup date.", result.Message);
        }

        [Fact]
        public async Task AddBidAsync_Should_Fail_When_Driver_Already_Has_Bid()
        {
            var request = new TransportRequest
            {
                TransportRequestId = 4,
                Status = ERequestStatus.Active,
                MaxPrice = 100,
                PickupDate = DateTime.Now,
                DeliveryDate = DateTime.Now.AddDays(5)
            };
            _ctx.TransportRequests.Add(request);
            await _ctx.SaveChangesAsync();

            var existing = new List<Bid>
            {
                new Bid { DriverId = 1, Status = EBidStatus.Pendent }
            };

            _mockRepo.Setup(r => r.GetByTransportRequestAsync(4))
                     .ReturnsAsync(existing);

            var dto = new AddBidDTO
            {
                TransportRequestId = 4,
                Value = 50,
                DeliveryDeadline = DateTime.Now.AddDays(3)
            };

            var result = await _service.AddBidAsync(1, dto);

            Assert.False(result.Success);
            Assert.Equal("Driver already has an active bid for this transport request.", result.Message);
        }

        [Fact]
        public async Task AddBidAsync_Should_Succeed_When_Valid()
        {
            var request = new TransportRequest
            {
                TransportRequestId = 5,
                Status = ERequestStatus.Active,
                MaxPrice = 100,
                PickupDate = DateTime.Now,
                DeliveryDate = DateTime.Now.AddDays(5)
            };
            _ctx.TransportRequests.Add(request);
            await _ctx.SaveChangesAsync();

            _mockRepo.Setup(r => r.GetByTransportRequestAsync(5))
                     .ReturnsAsync(new List<Bid>());
            _mockRepo.Setup(r => r.CreateAsync(It.IsAny<Bid>()))
                     .ReturnsAsync((Bid b) => b);

            var dto = new AddBidDTO
            {
                TransportRequestId = 5,
                Value = 80,
                DeliveryDeadline = DateTime.Now.AddDays(2)
            };

            var result = await _service.AddBidAsync(1, dto);

            Assert.True(result.Success);
            Assert.Equal(EBidStatus.Pendent, result.Bid.Status);
            Assert.Equal(1, result.Bid.DriverId);
            _mockRepo.Verify(r => r.CreateAsync(It.IsAny<Bid>()), Times.Once);
        }
    }
}
