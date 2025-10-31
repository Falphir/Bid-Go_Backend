using Bid_Go_Backend.Controllers;
using Bid_Go_Backend.Data;
using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.DTOs;
using Bid_Go_Backend.Data.Models.Enums;
using Bid_Go_Backend.Data.Repositories.Interfaces;
using Bid_Go_Backend.Repositories.Interface;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Bid_Go_Backend.Tests.Controllers
{
    public class BidsControllerTests
    {
        private readonly Mock<IBidsCRUD> _mockRepo;
        private readonly BidGoDbContext _context;
        private readonly BidsController _controller;

        public BidsControllerTests()
        {
            _mockRepo = new Mock<IBidsCRUD>();

            var options = new DbContextOptionsBuilder<BidGoDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new BidGoDbContext(options);
            _context.Database.EnsureCreated();

            _controller = new BidsController(_mockRepo.Object, _context);
        }

        [Fact]
        public async Task AddBid_ShouldReturnNotFound_WhenTransportRequestDoesNotExist()
        {
            // Arrange
            var bidDto = new BidDTO
            {
                TransportRequestId = 1,
                Value = 100,
                DeliveryDeadline = DateTime.UtcNow.AddDays(2),
                DriverId = 10
            };

            // Act
            var result = await _controller.AddBid(bidDto);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("Transport request not found.", notFoundResult.Value);
        }

        [Fact]
        public async Task AddBid_ShouldReturnBadRequest_WhenTransportRequestIsNotActive()
        {
            // Arrange
            var tr = new TransportRequest
            {
                TransportRequestId = 1,
                Status = ERequestStatus.Completed,
                PickupDate = DateTime.UtcNow,
                DeliveryDate = DateTime.UtcNow.AddDays(3)
            };

            _context.TransportRequests.Add(tr);
            _context.SaveChanges();

            var bidDto = new BidDTO
            {
                TransportRequestId = tr.TransportRequestId,
                Value = 100,
                DeliveryDeadline = DateTime.UtcNow.AddDays(1),
                DriverId = 1
            };

            // Act
            var result = await _controller.AddBid(bidDto);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Cannot place a bid on a transport request that is not open.", badRequest.Value);
        }

        [Fact]
        public async Task AddBid_ShouldReturnBadRequest_WhenBidValueIsZero()
        {
            var tr = new TransportRequest
            {
                TransportRequestId = 2,
                Status = ERequestStatus.Active,
                PickupDate = DateTime.UtcNow,
                DeliveryDate = DateTime.UtcNow.AddDays(2)
            };

            _context.TransportRequests.Add(tr);
            _context.SaveChanges();

            var bidDto = new BidDTO
            {
                TransportRequestId = tr.TransportRequestId,
                Value = 0,
                DeliveryDeadline = DateTime.UtcNow.AddDays(1),
                DriverId = 1
            };

            var result = await _controller.AddBid(bidDto);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Bid value must be greater than zero .", badRequest.Value);
        }

        [Fact]
        public async Task AddBid_ShouldReturnCreated_WhenValidBid()
        {
            var tr = new TransportRequest
            {
                TransportRequestId = 3,
                Status = ERequestStatus.Active,
                PickupDate = DateTime.UtcNow,
                DeliveryDate = DateTime.UtcNow.AddDays(5)
            };

            _context.TransportRequests.Add(tr);
            _context.SaveChanges();

            var bidDto = new BidDTO
            {
                TransportRequestId = tr.TransportRequestId,
                Value = 150,
                DeliveryDeadline = DateTime.UtcNow.AddDays(2),
                DriverId = 5
            };

            var bid = new Bid
            {
                BidId = 10,
                Value = bidDto.Value,
                DeliveryDeadline = bidDto.DeliveryDeadline,
                DriverId = bidDto.DriverId,
                TransportRequestId = bidDto.TransportRequestId
            };

            _mockRepo.Setup(r => r.CreateBidAsync(It.IsAny<Bid>()))
                .ReturnsAsync(bid);

            var result = await _controller.AddBid(bidDto);

            var created = Assert.IsType<CreatedAtActionResult>(result);
            var createdBid = Assert.IsType<Bid>(created.Value);
            Assert.Equal(bid.BidId, createdBid.BidId);
        }

        [Fact]
        public async Task CancelBid_ShouldReturnOk_WhenSuccessIsTrue()
        {
            _mockRepo.Setup(r => r.CancelBidAsync(1)).ReturnsAsync(true);

            var result = await _controller.CancelBid(1);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("Bid canceled successfully.", ok.Value);
        }

        [Fact]
        public async Task CancelBid_ShouldReturnNotFound_WhenFail()
        {
            _mockRepo.Setup(r => r.CancelBidAsync(1)).ReturnsAsync(false);

            var result = await _controller.CancelBid(1);

            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("Only pending bids can be canceled", notFound.Value);
        }

        [Fact]
        public async Task AddBid_ShouldReturnBadRequest_WhenDeliveryDeadlineBeforePickupDate()
        {
            var tr = new TransportRequest
            {
                TransportRequestId = 4,
                Status = ERequestStatus.Active,
                PickupDate = DateTime.UtcNow.AddDays(3),
                DeliveryDate = DateTime.UtcNow.AddDays(5)
            };

            _context.TransportRequests.Add(tr);
            _context.SaveChanges();

            var bidDto = new BidDTO
            {
                TransportRequestId = tr.TransportRequestId,
                Value = 100,
                DeliveryDeadline = DateTime.UtcNow.AddDays(2),
                DriverId = 1
            };

            var result = await _controller.AddBid(bidDto);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("The bid's delivery deadline need to be later than the transport request's pickup Date ", badRequest.Value);

}

        [Fact]
        public async Task AddBid_ShouldReturnBadRequest_WhenDeliveryDeadlineAfterDeliveryDate()
        {
            var tr = new TransportRequest
            {
                TransportRequestId = 5,
                Status = ERequestStatus.Active,
                PickupDate = DateTime.UtcNow,
                DeliveryDate = DateTime.UtcNow.AddDays(2)
            };


            _context.TransportRequests.Add(tr);
            _context.SaveChanges();

            var bidDto = new BidDTO
            {
                TransportRequestId = tr.TransportRequestId,
                Value = 120,
                DeliveryDeadline = DateTime.UtcNow.AddDays(3),
                DriverId = 1
            };

            var result = await _controller.AddBid(bidDto);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("The bid's delivery deadline cannot be later than the transport request's delivery date.", badRequest.Value);

}

        [Fact]
        public async Task AddBid_ShouldReturnBadRequest_WhenDriverHasActiveBid()
        {
            var tr = new TransportRequest
            {
                TransportRequestId = 6,
                Status = ERequestStatus.Active,
                PickupDate = DateTime.UtcNow,
                DeliveryDate = DateTime.UtcNow.AddDays(3)
            };

            _context.TransportRequests.Add(tr);
            _context.SaveChanges();

            _context.Bids.Add(new Bid
            {
                BidId = 1,
                DriverId = 10,
                TransportRequestId = tr.TransportRequestId,
                Status = EBidStatus.Pendent
            });
            _context.SaveChanges();

            var bidDto = new BidDTO
            {
                TransportRequestId = tr.TransportRequestId,
                Value = 200,
                DeliveryDeadline = DateTime.UtcNow.AddDays(1),
                DriverId = 10
            };

            var result = await _controller.AddBid(bidDto);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Driver already has an active bid for this transport request.", badRequest.Value);
}

        [Fact]
        public async Task UpdateBid_ShouldReturnNotFound_WhenBidCannotBeUpdated()
        {
            _mockRepo.Setup(r => r.UpdateBidAsync(It.IsAny<int>(), It.IsAny<Bid>()))
            .ReturnsAsync((Bid)null);

            var dto = new BidUpdateDTO { Value = 100, DeliveryDeadline = DateTime.UtcNow.AddDays(2) };

            var result = await _controller.UpdateBid(1, dto);

            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("Bid not found or cannot be updated.", notFound.Value);

}

        [Fact]
        public async Task UpdateBid_ShouldReturnOk_WhenUpdatedSuccessfully()
        {
            var updatedBid = new Bid { BidId = 2, Value = 150, DeliveryDeadline = DateTime.UtcNow.AddDays(3) };

            _mockRepo.Setup(r => r.UpdateBidAsync(2, It.IsAny<Bid>()))
                .ReturnsAsync(updatedBid);

            var dto = new BidUpdateDTO { Value = 150, DeliveryDeadline = updatedBid.DeliveryDeadline };

            var result = await _controller.UpdateBid(2, dto);

            var ok = Assert.IsType<OkObjectResult>(result);
            var bid = Assert.IsType<Bid>(ok.Value);
            Assert.Equal(150, bid.Value);
}

        [Fact]
        public async Task GetBidsById_ShouldReturnOk_WhenBidsFound()
        {
            var bid = new Bid { BidId = 10, Value = 100 };
            _mockRepo.Setup(r => r.GetBidByIdAsync(10)).ReturnsAsync(bid);

            var result = await _controller.GetBidsById(10);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(bid, ok.Value);

}

        [Fact]
        public async Task GetBidsById_ShouldReturnNotFound_WhenNoBidsFound()
        {
            _mockRepo.Setup(r => r.GetBidByIdAsync(10)).ReturnsAsync((Bid)null);

            var result = await _controller.GetBidsById(10);

            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("No bids found for the given transport request ID.", notFound.Value);

}

        [Fact]
        public async Task GetBidsByTransportRequest_ShouldReturnOk_WhenBidsExist()
        {
            var bids = new List<Bid> { new Bid { BidId = 1 }, new Bid { BidId = 2 } };
            _mockRepo.Setup(r => r.GetBidByTransportRequestAsync(5)).ReturnsAsync(bids);


            var result = await _controller.GetBidsByTransportRequest(5);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(bids, ok.Value);

}

        [Fact]
        public async Task GetBidsByTransportRequest_ShouldReturnNotFound_WhenNoBids()
        {
            _mockRepo.Setup(r => r.GetBidByTransportRequestAsync(5))
            .ReturnsAsync(new List<Bid>());

            var result = await _controller.GetBidsByTransportRequest(5);

            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("No bids found for the given transport request ID.", notFound.Value);


}

        [Fact]
        public async Task GetBidsByTransportRequestAndStatus_ShouldReturnOk_WhenBidsExist()
        {
            var bids = new List<Bid> { new Bid { BidId = 1 } };
            _mockRepo.Setup(r => r.GetBidByTransportRequestAndStatusAsync(1, EBidStatus.Pendent))
            .ReturnsAsync(bids);

            var result = await _controller.GetBidsByTransportRequestAndStatus(1, EBidStatus.Pendent);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(bids, ok.Value);

}

        [Fact]
        public async Task GetBidsByTransportRequestAndStatus_ShouldReturnNotFound_WhenNoBidsExist()
        {
            _mockRepo.Setup(r => r.GetBidByTransportRequestAndStatusAsync(1, EBidStatus.Pendent))
            .ReturnsAsync(new List<Bid>());

            var result = await _controller.GetBidsByTransportRequestAndStatus(1, EBidStatus.Pendent);

            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("No bids found for the given transport request ID and status.", notFound.Value);

        }

        [Fact]
        public async Task GetActiveBids_ShouldReturnOkWithProjectedResult_WhenActiveBidsExist()
        {
            // Arrange
            var driver = new Driver { Name = "DriverName", Email = "d@example.com" };
            var bids = new List<Bid>
            {
                new Bid
                {
                    BidId = 1,
                    Value = 99,
                    DeliveryDeadline = DateTime.UtcNow.AddDays(1),
                    DriverId = 10,
                    Driver = driver
                }
            };

            _mockRepo.Setup(r => r.GetActiveBidsByTransportRequestAsync(5, It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync(bids);

            // Act
            var result = await _controller.GetActiveBids(5, "value", false);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            var enumerable = Assert.IsAssignableFrom<IEnumerable<object>>(ok.Value);
            var list = enumerable.ToList();
            Assert.Single(list);

            var item = list[0];
            var itemType = item.GetType();

            var bidIdProp = itemType.GetProperty("BidId");
            Assert.NotNull(bidIdProp);
            Assert.Equal(1, (int)bidIdProp.GetValue(item));

            var valueProp = itemType.GetProperty("Value");
            Assert.NotNull(valueProp);
            Assert.Equal(99, Convert.ToDecimal(valueProp.GetValue(item)));

            var driverProp = itemType.GetProperty("Driver");
            Assert.NotNull(driverProp);
            var driverVal = driverProp.GetValue(item);
            var driverType = driverVal.GetType();

            var driverIdProp = driverType.GetProperty("DriverId");
            var driverNameProp = driverType.GetProperty("Name");
            var driverEmailProp = driverType.GetProperty("Email");

            Assert.NotNull(driverNameProp);
            Assert.Equal("DriverName", driverNameProp.GetValue(driverVal));
            Assert.NotNull(driverEmailProp);
            Assert.Equal("d@example.com", driverEmailProp.GetValue(driverVal));
        }

        [Fact]
        public async Task GetActiveBids_ShouldReturnOkWithMessage_WhenNoActiveBidsExist()
        {
            // Arrange
            _mockRepo.Setup(r => r.GetActiveBidsByTransportRequestAsync(5, It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync(new List<Bid>());

            // Act
            var result = await _controller.GetActiveBids(5, "value", false);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            var value = ok.Value;
            var valueType = value.GetType();

            var messageProp = valueType.GetProperty("message");
            Assert.NotNull(messageProp);
            Assert.Equal("No active bids found for this request.", messageProp.GetValue(value));

            var bidsProp = valueType.GetProperty("bids");
            Assert.NotNull(bidsProp);
            var bidsVal = bidsProp.GetValue(value) as IEnumerable<object>;
            Assert.NotNull(bidsVal);
            Assert.Empty(bidsVal);
        }

    }
}
