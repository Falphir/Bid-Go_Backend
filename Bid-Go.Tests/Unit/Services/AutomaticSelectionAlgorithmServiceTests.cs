using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.Enums;
using Bid_Go_Backend.Repositories.Interfaces;
using Bid_Go_Backend.Services.Bids;
using Bid_Go_Backend.Services.Interfaces;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Bid_Go.Tests.Unit.Services
{
    /// <summary>
    /// Unit tests for AutomaticSelectionAlgorithmService covering validation and selection logic.
    /// </summary>
    public class AutomaticSelectionAlgorithmServiceTests
    {
        private Mock<IAutomaticSelectionAlgorithmRepository> _mockRepo;
        private Mock<INotificationService> _mockNotif;
        private AutomaticSelectionAlgorithmService _service;

        public AutomaticSelectionAlgorithmServiceTests()
        {
            _mockRepo = new Mock<IAutomaticSelectionAlgorithmRepository>();
            _mockNotif = new Mock<INotificationService>();
            _service = new AutomaticSelectionAlgorithmService(_mockRepo.Object, _mockNotif.Object);
        }

        private TransportRequest MakeRequest(
            ERequestStatus status = ERequestStatus.Active,
            bool autoEnabled = true,
            DateTime? biddingEnd = null,
            List<Bid>? bids = null)
        {
            return new TransportRequest
            {
                TransportRequestId = 1,
                Status = status,
                IsAutomaticSelectionEnabled = autoEnabled,
                BiddingEndDate = biddingEnd ?? DateTime.UtcNow.AddDays(-1),
                Bids = bids ?? new List<Bid>()
            };
        }

        private Bid MakeBid(int bidId, int driverId, decimal value, EBidStatus status = EBidStatus.Pendent)
        {
            return new Bid
            {
                BidId = bidId,
                DriverId = driverId,
                Value = value,
                Status = status,
                TransportRequestId = 1
            };
        }

        [Fact]
        public async Task ExecuteAsync_ShouldReturnFalse_WhenTransportRequestNotFound()
        {
            // Arrange
            _mockRepo.Setup(r => r.GetTransportRequestWithBidsAsync(1))
                     .ReturnsAsync((TransportRequest?)null);

            // Act
            var (success, message, bid) = await _service.ExecuteAsync(1);

            // Assert
            Assert.False(success);
            Assert.Equal("Transport request not found.", message);
            Assert.Null(bid);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldReturnFalse_WhenAutomaticSelectionDisabled()
        {
            var tr = MakeRequest(autoEnabled: false);
            _mockRepo.Setup(r => r.GetTransportRequestWithBidsAsync(1)).ReturnsAsync(tr);

            var (success, message, bid) = await _service.ExecuteAsync(1);

            Assert.False(success);
            Assert.Equal("Automatic selection is not enabled.", message);
            Assert.Null(bid);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldReturnFalse_WhenBiddingNotFinished()
        {
            var tr = MakeRequest(biddingEnd: DateTime.UtcNow.AddHours(1));
            _mockRepo.Setup(r => r.GetTransportRequestWithBidsAsync(1)).ReturnsAsync(tr);

            var (success, message, bid) = await _service.ExecuteAsync(1);

            Assert.False(success);
            Assert.Equal("Bidding has not finished yet.", message);
            Assert.Null(bid);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldReturnFalse_WhenTransportRequestNotActive()
        {
            var tr = MakeRequest(status: ERequestStatus.Canceled);
            _mockRepo.Setup(r => r.GetTransportRequestWithBidsAsync(1)).ReturnsAsync(tr);

            var (success, message, bid) = await _service.ExecuteAsync(1);

            Assert.False(success);
            Assert.Equal("The transport request is not active.", message);
            Assert.Null(bid);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldReturnFalse_WhenBidAlreadyAccepted()
        {
            var bids = new List<Bid> { MakeBid(1, 10, 100, EBidStatus.Accepted) };
            var tr = MakeRequest(bids: bids);
            _mockRepo.Setup(r => r.GetTransportRequestWithBidsAsync(1)).ReturnsAsync(tr);

            var (success, message, bid) = await _service.ExecuteAsync(1);

            Assert.False(success);
            Assert.Equal("There is already an accepted bid for this request.", message);
            Assert.Null(bid);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldReturnFalse_WhenNoBids()
        {
            var tr = MakeRequest();
            _mockRepo.Setup(r => r.GetTransportRequestWithBidsAsync(1)).ReturnsAsync(tr);

            var (success, message, bid) = await _service.ExecuteAsync(1);

            Assert.False(success);
            Assert.Equal("No bids submitted.", message);
            Assert.Null(bid);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldReturnFalse_WhenNoEligibleBids()
        {
            var bids = new List<Bid> { MakeBid(1, 10, 100) };
            var tr = MakeRequest(bids: bids);

            _mockRepo.Setup(r => r.GetTransportRequestWithBidsAsync(1)).ReturnsAsync(tr);
            _mockRepo.Setup(r => r.GetDriverReputationsAsync(It.IsAny<IEnumerable<int>>()))
                     .ReturnsAsync(new Dictionary<int, decimal> { { 10, 2 } });

            var (success, message, bid) = await _service.ExecuteAsync(1);

            Assert.False(success);
            Assert.Equal("No eligible bids.", message);
            Assert.Null(bid);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldSelectBidAndRejectOthers()
        {
            var bids = new List<Bid>
            {
                MakeBid(1, 10, 100),
                MakeBid(2, 11, 90),
                MakeBid(3, 12, 90) // same min price, tie-breaker by ID
            };
            var tr = MakeRequest(bids: bids);

            _mockRepo.Setup(r => r.GetTransportRequestWithBidsAsync(1)).ReturnsAsync(tr);
            _mockRepo.Setup(r => r.GetDriverReputationsAsync(It.IsAny<IEnumerable<int>>()))
                     .ReturnsAsync(new Dictionary<int, decimal> { { 10, 5 }, { 11, 4 }, { 12, 4 } });
            _mockRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        _mockNotif.Setup(n => n.CreateAndSendAsync(
        It.IsAny<int>(),
        It.IsAny<string>(),
        It.IsAny<ENotificationType>(),
        It.IsAny<int>(),
        It.IsAny<int>()
    ))
    .ReturnsAsync(new Notification());


        var (success, message, selectedBid) = await _service.ExecuteAsync(1);

            Assert.True(success);
            Assert.Null(message);
            Assert.NotNull(selectedBid);
            Assert.Equal(2, selectedBid!.BidId); // lowest value & tiebreaker by ID
            Assert.Equal(EBidStatus.Accepted, selectedBid.Status);

            // Outros bids devem estar rejeitados
            Assert.All(bids.Where(b => b.BidId != selectedBid.BidId), b => Assert.Equal(EBidStatus.Rejected, b.Status));

            _mockNotif.Verify(n => n.CreateAndSendAsync(selectedBid.DriverId, It.IsAny<string>(), ENotificationType.Accepted, selectedBid.BidId, tr.TransportRequestId), Times.Once);
            _mockNotif.Verify(n => n.CreateAndSendAsync(It.Is<int>(d => d != selectedBid.DriverId), It.IsAny<string>(), ENotificationType.Rejected, It.IsAny<int>(), tr.TransportRequestId), Times.Exactly(2));
            _mockRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldFail_WhenSelectedBidNotPending()
        {
            var bids = new List<Bid>
            {
                MakeBid(1, 10, 100, EBidStatus.Accepted)
            };
            var tr = MakeRequest(bids: bids);

            _mockRepo.Setup(r => r.GetTransportRequestWithBidsAsync(1)).ReturnsAsync(tr);
            _mockRepo.Setup(r => r.GetDriverReputationsAsync(It.IsAny<IEnumerable<int>>()))
                     .ReturnsAsync(new Dictionary<int, decimal> { { 10, 5 } });

            var (success, message, selectedBid) = await _service.ExecuteAsync(1);

            Assert.False(success);
        Assert.Equal("There is already an accepted bid for this request.", message);
        Assert.Null(selectedBid);
        }
    }
}
