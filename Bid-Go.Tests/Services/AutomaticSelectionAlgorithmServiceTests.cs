using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Repositories.Interfaces;
using Bid_Go_Backend.Services.Bids;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Bid_Go.Tests.Services
{
    public class AutomaticSelectionAlgorithmServiceTests
    {
        private readonly Mock<IAutomaticSelectionAlgorithmRepository> _mockRepo;
        private readonly AutomaticSelectionAlgorithmService _service;

        public AutomaticSelectionAlgorithmServiceTests()
        {
            _mockRepo = new Mock<IAutomaticSelectionAlgorithmRepository>();
            _service = new AutomaticSelectionAlgorithmService(_mockRepo.Object);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldReturnFailure_WhenSelectionFails()
        {
            // arrange
            var repoResult = new AutomaticSelectionResult
            {
                SelectedBid = null,
                Message = "No eligible bids"
            };
            _mockRepo.Setup(r => r.ExecuteAutomaticSelectionAsync(42))
                     .ReturnsAsync(repoResult);

            // act
            var (success, message, selectedBid) = await _service.ExecuteAsync(42);

            // assert
            Assert.False(success);
            Assert.Equal("No eligible bids", message);
            Assert.Null(selectedBid);
            _mockRepo.Verify(r => r.ExecuteAutomaticSelectionAsync(42), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldReturnSuccess_WithSelectedBid_WhenSuccess()
        {
            // arrange
            var driver = new Driver
            {
                Id = 5,
                Name = "DriverName",
                Email = "d@example.com",
                PhoneNumber = 912345678
            };
            var bid = new Bid
            {
                BidId = 1,
                Value = 99.50m,
                DriverId = 5,
                Driver = driver
            };
            var repoResult = new AutomaticSelectionResult
            {
                SelectedBid = bid,
                Message = "Selected"
            };
            _mockRepo.Setup(r => r.ExecuteAutomaticSelectionAsync(100))
                     .ReturnsAsync(repoResult);

            // act
            var (success, message, selectedBid) = await _service.ExecuteAsync(100);

            // assert
            Assert.True(success);
            Assert.Equal("Selected", message);
            Assert.NotNull(selectedBid);
            Assert.Equal(1, selectedBid!.BidId);
            Assert.Equal(99.50m, selectedBid.Value);
            Assert.NotNull(selectedBid.Driver);
            Assert.Equal(5, selectedBid.DriverId);
            Assert.Equal("DriverName", selectedBid.Driver!.Name);
            Assert.Equal("d@example.com", selectedBid.Driver.Email);
            Assert.Equal(912345678, selectedBid.Driver.PhoneNumber);

            _mockRepo.Verify(r => r.ExecuteAutomaticSelectionAsync(100), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldThrow_WhenRepositoryReturnsNull()
        {
            // arrange
            _mockRepo.Setup(r => r.ExecuteAutomaticSelectionAsync(7))
                     .ReturnsAsync((AutomaticSelectionResult?)null);

            // act/assert
            await Assert.ThrowsAsync<NullReferenceException>(async () =>
                await _service.ExecuteAsync(7)
            );

            _mockRepo.Verify(r => r.ExecuteAutomaticSelectionAsync(7), Times.Once);
        }
    }
}
