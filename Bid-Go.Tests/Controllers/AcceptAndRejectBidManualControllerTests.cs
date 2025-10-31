using Bid_Go_Backend.Controllers;
using Bid_Go_Backend.Data;
using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.Enums;
using Bid_Go_Backend.Data.Repositories.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Bid_Go.Tests.Controllers
{
    public class AcceptAndRejectBidManualControllerTests
    {
        private readonly Mock<IAcceptAndRejectBidManual> _mockService;
        private readonly BidGoDbContext _ctx;
        private readonly AcceptAndRejectBidManualController _controller;

        public AcceptAndRejectBidManualControllerTests()
        {
            _mockService = new Mock<IAcceptAndRejectBidManual>();

            var options = new DbContextOptionsBuilder<BidGoDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _ctx = new BidGoDbContext(options);
            _ctx.Database.EnsureCreated();

            _controller = new AcceptAndRejectBidManualController(_mockService.Object, _ctx);
        }

        [Fact]
        public async Task GetBidsById_ShouldReturnNotFound_WhenBidDoesNotExist()
        {
            // Arrange
            int bidId = 1;
            _mockService
                .Setup(s => s.GetBidByIdAsync(bidId))
                .ReturnsAsync((Bid)null);

            // Act
            var result = await _controller.GetBidsById(bidId);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task GetBidsById_ShouldReturnOk_WhenBidExists()
        {
            // Arrange
            int bidId = 1;
            var bid = new Bid
            {
                BidId = bidId,
                Value = 100,
                DriverId = 2,
                TransportRequestId = 10,
                Status = EBidStatus.Pendent
            };

            _mockService
                .Setup(s => s.GetBidByIdAsync(bidId))
                .ReturnsAsync(bid);

            // Act
            var result = await _controller.GetBidsById(bidId);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(StatusCodes.Status200OK, ok.StatusCode);
            Assert.Equal(bid, ok.Value);
        }

        [Fact]
        public async Task GetBidsByTransportRequest_ShouldReturnNotFound_WhenNoBids()
        {
            // Arrange
            int trId = 99;

            _mockService
                .Setup(s => s.GetBidByTransportRequestAsync(trId))
                .ReturnsAsync((List<Bid>)null);

            // Act
            var result = await _controller.GetBidsByTransportRequest(trId);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task GetBidsByTransportRequest_ShouldReturnNotFound_WhenEmptyList()
        {
            // Arrange
            int trId = 99;

            _mockService
                .Setup(s => s.GetBidByTransportRequestAsync(trId))
                .ReturnsAsync(new List<Bid>());

            // Act
            var result = await _controller.GetBidsByTransportRequest(trId);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task GetBidsByTransportRequest_ShouldReturnOk_WhenBidsExist()
        {
            // Arrange
            int trId = 99;

            var bids = new List<Bid>
            {
                new Bid { BidId = 1, TransportRequestId = trId, Value = 100 },
                new Bid { BidId = 2, TransportRequestId = trId, Value = 120 },
            };

            _mockService
                .Setup(s => s.GetBidByTransportRequestAsync(trId))
                .ReturnsAsync(bids);

            // Act
            var result = await _controller.GetBidsByTransportRequest(trId);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(StatusCodes.Status200OK, ok.StatusCode);
            Assert.Equal(bids, ok.Value);
        }

        [Fact]
        public async Task GetBidsByTransportRequestAndStatus_ShouldReturnNotFound_WhenNoBids()
        {
            // Arrange
            int trId = 10;
            var status = EBidStatus.Accepted;

            _mockService
                .Setup(s => s.GetBidByTransportRequestAndStatusAsync(trId, status))
                .ReturnsAsync((IEnumerable<Bid>)null);

            // Act
            var result = await _controller.GetBidsByTransportRequestAndStatus(trId, status);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task GetBidsByTransportRequestAndStatus_ShouldReturnOk_WhenBidsExist()
        {
            // Arrange
            int trId = 10;
            var status = EBidStatus.Accepted;

            var bids = new List<Bid>
            {
                new Bid { BidId = 1, TransportRequestId = trId, Status = status },
                new Bid { BidId = 2, TransportRequestId = trId, Status = status }
            };

            _mockService
                .Setup(s => s.GetBidByTransportRequestAndStatusAsync(trId, status))
                .ReturnsAsync(bids);

            // Act
            var result = await _controller.GetBidsByTransportRequestAndStatus(trId, status);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(StatusCodes.Status200OK, ok.StatusCode);
            Assert.Equal(bids, ok.Value);
        }

        [Fact]
        public async Task AcceptBid_ShouldReturnOk_WhenServiceRuns()
        {
            // Arrange
            int bidId = 5;

            _mockService
                .Setup(s => s.AcceptBidAsync(bidId))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.AcceptBid(bidId);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(StatusCodes.Status200OK, ok.StatusCode);
        }

        [Fact]
        public async Task AcceptBid_ShouldReturnBadRequest_WhenServiceThrows()
        {
            // Arrange
            int bidId = 5;

            _mockService
                .Setup(s => s.AcceptBidAsync(bidId))
                .ThrowsAsync(new Exception("cannot accept"));

            // Act
            var result = await _controller.AcceptBid(bidId);

            // Assert
            var bad = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, bad.StatusCode);
        }

        [Fact]
        public async Task RejectBid_ShouldReturnOk_WhenServiceRuns()
        {
            // Arrange
            int bidId = 7;

            _mockService
                .Setup(s => s.RejectBidAsync(bidId))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.RejectedBid(bidId);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(StatusCodes.Status200OK, ok.StatusCode);
        }

        [Fact]
        public async Task RejectBid_ShouldReturnBadRequest_WhenServiceThrows()
        {
            // Arrange
            int bidId = 7;

            _mockService
                .Setup(s => s.RejectBidAsync(bidId))
                .ThrowsAsync(new Exception("cannot reject"));

            // Act
            var result = await _controller.RejectedBid(bidId);

            // Assert
            var bad = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, bad.StatusCode);
        }
    }
}