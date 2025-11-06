using Bid_Go_Backend.Data;
using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.DTOs;
using Bid_Go_Backend.Data.Models.Enums;
using Bid_Go_Backend.Repositories.Interfaces;
using Bid_Go_Backend.Services;
using Bid_Go_Backend.Services.Bids;
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

            // Criar um DbContext em memória
            var options = new DbContextOptionsBuilder<BidGoDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _ctx = new BidGoDbContext(options);
            _service = new BidsService(_mockRepo.Object, _ctx);
        }

        // --- ADD BID TESTS ---

        [Fact]
        public async Task AddBidAsync_Should_Fail_When_TransportRequest_NotFound()
        {
            // Arrange
            var dto = new AddBidDTO { TransportRequestId = 1 };

            // Act
            var result = await _service.AddBidAsync(dto);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Transport request not found.", result.Message);
        }

        [Fact]
        public async Task AddBidAsync_Should_Fail_When_Request_Not_Active()
        {
            // Arrange
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

            // Act
            var result = await _service.AddBidAsync(dto);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Cannot place a bid on a transport request that is not open.", result.Message);
        }

        [Fact]
        public async Task AddBidAsync_Should_Fail_When_Value_Invalid()
        {
            // Arrange
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
                DriverId = 1,
                Value = -5,
                DeliveryDeadline = DateTime.Now.AddDays(3)
            };

            // Act
            var result = await _service.AddBidAsync(dto);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Bid value must be greater than zero.", result.Message);
        }

        [Fact]
        public async Task AddBidAsync_Should_Fail_When_Deadline_Invalid()
        {
            // Arrange
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
                DriverId = 1,
                Value = 50,
                DeliveryDeadline = request.PickupDate.AddHours(-1)
            };

            // Act
            var result = await _service.AddBidAsync(dto);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("The bid's delivery deadline must be later than the pickup date.", result.Message);
        }

        [Fact]
        public async Task AddBidAsync_Should_Fail_When_Driver_Already_Has_Bid()
        {
            // Arrange
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

            var existing = new List<Bid> {
                new Bid { DriverId = 1, Status = EBidStatus.Pendent }
            };

            _mockRepo.Setup(r => r.GetByTransportRequestAsync(4))
                     .ReturnsAsync(existing);

            var dto = new AddBidDTO
            {
                TransportRequestId = 4,
                DriverId = 1,
                Value = 50,
                DeliveryDeadline = DateTime.Now.AddDays(3)
            };

            // Act
            var result = await _service.AddBidAsync(dto);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Driver already has an active bid for this transport request.", result.Message);
        }

        [Fact]
        public async Task AddBidAsync_Should_Succeed_When_Valid()
        {
            // Arrange
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
                DriverId = 2,
                Value = 80,
                DeliveryDeadline = DateTime.Now.AddDays(2)
            };

            // Act
            var result = await _service.AddBidAsync(dto);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(EBidStatus.Pendent, result.Bid.Status);
            _mockRepo.Verify(r => r.CreateAsync(It.IsAny<Bid>()), Times.Once);
        }

        // --- UPDATE BID TESTS ---

        [Fact]
        public async Task UpdateBidAsync_Should_Fail_When_Bid_NotFound()
        {
            _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Bid)null);

            var result = await _service.UpdateBidAsync(1, new BidUpdateDTO());
            Assert.False(result.Success);
            Assert.Equal("Bid not found.", result.Message);
        }

        [Fact]
        public async Task UpdateBidAsync_Should_Fail_When_Bid_Not_Pending()
        {
            _mockRepo.Setup(r => r.GetByIdAsync(1))
                     .ReturnsAsync(new Bid { Status = EBidStatus.Accepted });

            var result = await _service.UpdateBidAsync(1, new BidUpdateDTO());
            Assert.False(result.Success);
            Assert.Equal("Only pending bids can be updated.", result.Message);
        }

        [Fact]
        public async Task UpdateBidAsync_Should_Succeed_When_Valid()
        {
            var request = new TransportRequest
            {
                TransportRequestId = 7,
                Status = ERequestStatus.Active,
                MaxPrice = 200,
                PickupDate = DateTime.Now,
                DeliveryDate = DateTime.Now.AddDays(5)
            };
            _ctx.TransportRequests.Add(request);
            await _ctx.SaveChangesAsync();

            var bid = new Bid
            {
                BidId = 1,
                TransportRequestId = 7,
                Status = EBidStatus.Pendent,
                Value = 50,
                DeliveryDeadline = DateTime.Now.AddDays(2)
            };
            _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(bid);
            _mockRepo
    .Setup(r => r.UpdateAsync(It.IsAny<Bid>()))
    .ReturnsAsync((Bid b) => b);


            var dto = new BidUpdateDTO
            {
                Value = 100,
                DeliveryDeadline = DateTime.Now.AddDays(3)
            };

            var result = await _service.UpdateBidAsync(1, dto);

            Assert.True(result.Success);
            Assert.Equal(100, bid.Value);
            _mockRepo.Verify(r => r.UpdateAsync(It.IsAny<Bid>()), Times.Once);
        }

        // --- CANCEL BID TESTS ---

        [Fact]
        public async Task CancelBidAsync_Should_Fail_When_Bid_NotFound()
        {
            _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Bid)null);

            var result = await _service.CancelBidAsync(1);

            Assert.False(result.Success);
            Assert.Equal("Bid not found.", result.Message);
        }

        [Fact]
        public async Task CancelBidAsync_Should_Fail_When_Bid_Not_Pending()
        {
            var bid = new Bid { Status = EBidStatus.Accepted };
            _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(bid);

            var result = await _service.CancelBidAsync(1);

            Assert.False(result.Success);
            Assert.Equal("Only pending bids can be canceled.", result.Message);
        }

        [Fact]
        public async Task CancelBidAsync_Should_Succeed_When_Valid()
        {
            var bid = new Bid { Status = EBidStatus.Pendent };
            _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(bid);
            _mockRepo
    .Setup(r => r.UpdateAsync(It.IsAny<Bid>()))
    .ReturnsAsync((Bid b) => b);


            var result = await _service.CancelBidAsync(1);

            Assert.True(result.Success);
            Assert.Equal(EBidStatus.Canceled, bid.Status);
            _mockRepo.Verify(r => r.UpdateAsync(It.IsAny<Bid>()), Times.Once);
        }

        [Fact]
        public async Task GetBidsByTransportRequestAsync_ShouldReturnListOfBids()
        {
            // Arrange
            int transportRequestId = 1;
            var expectedBids = new List<Bid> { new Bid { BidId = 1 }, new Bid { BidId = 2 } };

            _mockRepo.Setup(r => r.GetByTransportRequestAsync(transportRequestId))
                     .ReturnsAsync(expectedBids);

            // Act
            var result = await _service.GetBidsByTransportRequestAsync(transportRequestId);

            // Assert
            Assert.Equal(expectedBids, result);
            _mockRepo.Verify(r => r.GetByTransportRequestAsync(transportRequestId), Times.Once);
        }

        [Fact]
        public async Task GetBidsByTransportRequestAndStatusAsync_ShouldReturnFilteredBids()
        {
            // Arrange
            int transportRequestId = 1;
            var status = EBidStatus.Accepted;
            var expectedBids = new List<Bid> { new Bid { BidId = 3, Status = status } };

            _mockRepo.Setup(r => r.GetByTransportRequestAndStatusAsync(transportRequestId, status))
                     .ReturnsAsync(expectedBids);

            // Act
            var result = await _service.GetBidsByTransportRequestAndStatusAsync(transportRequestId, status);

            // Assert
            Assert.Equal(expectedBids, result);
            _mockRepo.Verify(r => r.GetByTransportRequestAndStatusAsync(transportRequestId, status), Times.Once);
        }

        [Fact]
        public async Task GetActiveBidsAsync_ShouldReturnOrderedBids()
        {
            // Arrange
            int transportRequestId = 1;
            string orderBy = "value";
            bool descending = true;
            var expectedBids = new List<Bid> { new Bid { BidId = 5 }, new Bid { BidId = 6 } };

            _mockRepo.Setup(r => r.GetActiveBidsAsync(transportRequestId, orderBy, descending))
                     .ReturnsAsync(expectedBids);

            // Act
            var result = await _service.GetActiveBidsAsync(transportRequestId, orderBy, descending);

            // Assert
            Assert.Equal(expectedBids, result);
            _mockRepo.Verify(r => r.GetActiveBidsAsync(transportRequestId, orderBy, descending), Times.Once);
        }
    }
}

