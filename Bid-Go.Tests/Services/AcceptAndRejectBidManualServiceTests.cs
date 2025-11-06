using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.Enums;
using Bid_Go_Backend.Repositories.Interfaces;
using Bid_Go_Backend.Services.Bids;
using Bid_Go_Backend.Services.Interfaces;
using Moq;
using Xunit;

namespace Bid_Go.Tests.Services
{
    public class AcceptAndRejectBidManualServiceTests
    {
        private readonly Mock<IAcceptAndRejectBidManualRepository> _mockRepo;
        private readonly Mock<INotificationService> _mockNotification;
        private readonly AcceptAndRejectBidManualService _service;

        public AcceptAndRejectBidManualServiceTests()
        {
            _mockRepo = new Mock<IAcceptAndRejectBidManualRepository>();
            _mockNotification = new Mock<INotificationService>();
            _service = new AcceptAndRejectBidManualService(_mockRepo.Object, _mockNotification.Object);
        }

        // --- Get Methods ---

        [Fact]
        public async Task GetBidByIdAsync_Should_Call_Repo_And_Return_Bid()
        {
            // Arrange
            var bid = new Bid { BidId = 1 };
            _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(bid);

            // Act
            var result = await _service.GetBidByIdAsync(1);

            // Assert
            Assert.Equal(bid, result);
            _mockRepo.Verify(r => r.GetByIdAsync(1), Times.Once);
        }

        [Fact]
        public async Task GetBidsByTransportRequestAsync_Should_Return_List()
        {
            // Arrange
            var list = new List<Bid> { new Bid { BidId = 1 }, new Bid { BidId = 2 } };
            _mockRepo.Setup(r => r.GetByTransportRequestAsync(10)).ReturnsAsync(list);

            // Act
            var result = await _service.GetBidsByTransportRequestAsync(10);

            // Assert
            Assert.Equal(2, result.Count);
        }

        // --- Accept Bid ---

        [Fact]
        public async Task AcceptBidAsync_Should_Throw_When_Bid_NotFound()
        {
            _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Bid)null);

            await Assert.ThrowsAsync<Exception>(() => _service.AcceptBidAsync(1));
        }

        [Fact]
        public async Task AcceptBidAsync_Should_Throw_When_Bid_Not_Pending()
        {
            var bid = new Bid { BidId = 1, Status = EBidStatus.Accepted };
            _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(bid);

            await Assert.ThrowsAsync<Exception>(() => _service.AcceptBidAsync(1));
        }

        [Fact]
        public async Task AcceptBidAsync_Should_Throw_When_TransportRequest_Null()
        {
            var bid = new Bid { BidId = 1, Status = EBidStatus.Pendent, TransportRequest = null };
            _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(bid);

            await Assert.ThrowsAsync<Exception>(() => _service.AcceptBidAsync(1));
        }

        [Fact]
        public async Task AcceptBidAsync_Should_Throw_When_TransportRequest_Not_Active()
        {
            var bid = new Bid
            {
                BidId = 1,
                Status = EBidStatus.Pendent,
                TransportRequest = new TransportRequest { Status = ERequestStatus.Pending }
            };
            _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(bid);

            await Assert.ThrowsAsync<Exception>(() => _service.AcceptBidAsync(1));
        }

        [Fact]
        public async Task AcceptBidAsync_Should_Throw_When_Already_Accepted_Bid_Exists()
        {
            var bid = new Bid
            {
                BidId = 1,
                Status = EBidStatus.Pendent,
                TransportRequestId = 10,
                TransportRequest = new TransportRequest { Status = ERequestStatus.Active }
            };

            _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(bid);
            _mockRepo.Setup(r => r.GetByTransportRequestAndStatusAsync(10, EBidStatus.Accepted))
                     .ReturnsAsync(new List<Bid> { new Bid() });

            await Assert.ThrowsAsync<Exception>(() => _service.AcceptBidAsync(1));
        }

        [Fact]
        public async Task AcceptBidAsync_Should_Update_Bid_And_Reject_Others()
        {
            // Arrange
            var bid = new Bid
            {
                BidId = 1,
                DriverId = 5,
                Status = EBidStatus.Pendent,
                TransportRequestId = 10,
                TransportRequest = new TransportRequest
                {
                    TransportRequestId = 10,
                    Status = ERequestStatus.Active
                }
            };

            var otherBids = new List<Bid>
            {
                new Bid { BidId = 2, DriverId = 6, TransportRequestId = 10, Status = EBidStatus.Pendent },
                new Bid { BidId = 3, DriverId = 7, TransportRequestId = 10, Status = EBidStatus.Pendent }
            };

            _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(bid);
            _mockRepo.Setup(r => r.GetByTransportRequestAndStatusAsync(10, EBidStatus.Accepted))
                     .ReturnsAsync(new List<Bid>());
            _mockRepo.Setup(r => r.GetByTransportRequestAsync(10)).ReturnsAsync(new List<Bid>(otherBids));

            // Act
            await _service.AcceptBidAsync(1);

            // Assert
            Assert.Equal(EBidStatus.Accepted, bid.Status);
            Assert.All(otherBids, b => Assert.Equal(EBidStatus.Rejected, b.Status));
            Assert.Equal(ERequestStatus.Pending, bid.TransportRequest.Status);

            _mockRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
            _mockNotification.Verify(n => n.CreateAndSendAsync(
                bid.DriverId,
                It.IsAny<string>(),
                ENotificationType.Accepted,
                bid.BidId,
                bid.TransportRequestId
            ), Times.Once);
        }

        // --- Reject Bid ---

        [Fact]
        public async Task RejectBidAsync_Should_Throw_When_Bid_NotFound()
        {
            _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Bid)null);
            await Assert.ThrowsAsync<Exception>(() => _service.RejectBidAsync(1));
        }

        [Fact]
        public async Task RejectBidAsync_Should_Throw_When_Bid_Not_Pending()
        {
            var bid = new Bid { BidId = 1, Status = EBidStatus.Accepted };
            _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(bid);
            await Assert.ThrowsAsync<Exception>(() => _service.RejectBidAsync(1));
        }

        [Fact]
        public async Task RejectBidAsync_Should_Reject_Bid_And_Send_Notification()
        {
            // Arrange
            var bid = new Bid
            {
                BidId = 1,
                DriverId = 2,
                Status = EBidStatus.Pendent,
                TransportRequest = new TransportRequest { Status = ERequestStatus.Active },
                TransportRequestId = 9
            };

            _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(bid);

            // Act
            await _service.RejectBidAsync(1);

            // Assert
            Assert.Equal(EBidStatus.Rejected, bid.Status);
            _mockRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
            _mockNotification.Verify(n => n.CreateAndSendAsync(
                bid.DriverId,
                It.IsAny<string>(),
                ENotificationType.Rejected,
                bid.BidId,
                bid.TransportRequestId
            ), Times.Once);
        }
    }
}
