using Bid_Go_Backend.Data.Models.DTOs;
using Bid_Go_Backend.Data.Models.Enums;
using Bid_Go_Backend.Repositories.Interfaces;
using Bid_Go_Backend.Services.History;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Bid_Go_Backend.Tests.Services
{
    public class HistoryServiceTests
    {
        private readonly Mock<IHistoryRepository> _mockRepo;
        private readonly Mock<ILogger<HistoryService>> _mockLogger;
        private readonly HistoryService _service;

        public HistoryServiceTests()
        {
            _mockRepo = new Mock<IHistoryRepository>();
            _mockLogger = new Mock<ILogger<HistoryService>>();
            _service = new HistoryService(_mockRepo.Object, _mockLogger.Object);
        }

        // GetDriverHistoryAsync
        [Fact]
        public async Task GetDriverHistoryAsync_ShouldReturnList_WhenRepositoryReturnsData()
        {
            var expected = new List<BidHistoryDTO>
            {
                new BidHistoryDTO
                {
                    CompanyName = "Co",
                    Package = "Box",
                    Date = DateTime.UtcNow,
                    Destination = "Porto",
                    Value =120,
                    Status = EBidStatus.Accepted,
                    Rating =4
                }
            };

            _mockRepo.Setup(r => r.GetDriverHistoryAsync(1)).ReturnsAsync(expected);

            var result = await _service.GetDriverHistoryAsync(1);

            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal("Co", result[0].CompanyName);
        }

        [Fact]
        public async Task GetDriverHistoryAsync_ShouldReturnNull_WhenRepositoryReturnsNull()
        {
            _mockRepo.Setup(r => r.GetDriverHistoryAsync(2)).ReturnsAsync((List<BidHistoryDTO>?)null);

            var result = await _service.GetDriverHistoryAsync(2);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetDriverHistoryAsync_ShouldPropagateException_WhenRepositoryThrows()
        {
            _mockRepo.Setup(r => r.GetDriverHistoryAsync(3)).ThrowsAsync(new Exception("fail"));

            await Assert.ThrowsAsync<Exception>(() => _service.GetDriverHistoryAsync(3));
        }

        // GetTransportHistoryAsync
        [Fact]
        public async Task GetTransportHistoryAsync_ShouldReturnList_WhenRepositoryReturnsData()
        {
            var expected = new List<TransportHistoryDTO>
            {
                new TransportHistoryDTO
                {
                    TransportRequestId =1,
                    Package = "Box",
                    Name = "Cliente",
                    Date = DateTime.UtcNow,
                    Destination = "Lisboa",
                    Price =20,
                    Status = "Concluído"
                }
            };

            _mockRepo.Setup(r => r.GetTransportHistoryAsync(10)).ReturnsAsync(expected);

            var result = await _service.GetTransportHistoryAsync(10);

            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(1, result[0].TransportRequestId);
        }

        [Fact]
        public async Task GetTransportHistoryAsync_ShouldReturnEmpty_WhenRepositoryReturnsEmptyList()
        {
            _mockRepo.Setup(r => r.GetTransportHistoryAsync(11)).ReturnsAsync(new List<TransportHistoryDTO>());

            var result = await _service.GetTransportHistoryAsync(11);

            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetTransportHistoryAsync_ShouldPropagateException_WhenRepositoryThrows()
        {
            _mockRepo.Setup(r => r.GetTransportHistoryAsync(12)).ThrowsAsync(new Exception("fail"));

            await Assert.ThrowsAsync<Exception>(() => _service.GetTransportHistoryAsync(12));
        }
    }
}