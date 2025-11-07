using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Repositories.Interfaces;
using Bid_Go_Backend.Services.Transport_Request;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Bid_Go.Tests.Unit.Services
{
    public class TransportRequestsPageServiceTests
    {
        private readonly Mock<ITransportRequestsPageRepository> _repositoryMock;
        private readonly Mock<ILogger<TransportRequestsPageService>> _loggerMock;
        private readonly TransportRequestsPageService _service;

        public TransportRequestsPageServiceTests()
        {
            _repositoryMock = new Mock<ITransportRequestsPageRepository>();
            _loggerMock = new Mock<ILogger<TransportRequestsPageService>>();
            _service = new TransportRequestsPageService(_repositoryMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task GetActiveAsync_ShouldCallRepositoryAndReturnResults()
        {
            // Arrange
            var list = new List<TransportRequest>
        {
            new TransportRequest { TransportRequestId = 1, Origin = "Lisboa", Destination = "Porto" },
            new TransportRequest { TransportRequestId = 2, Origin = "Lisboa", Destination = "Braga" }
        };
            _repositoryMock.Setup(r => r.GetActiveAsync("Lisboa", null, null, "asc"))
                           .ReturnsAsync(list);

            // Act
            var result = await _service.GetActiveAsync("Lisboa", null, null, "asc");

            // Assert
            Assert.Equal(2, result.Count());
            _repositoryMock.Verify(r => r.GetActiveAsync("Lisboa", null, null, "asc"), Times.Once);
        }

        [Fact]
        public async Task GetActiveAsync_ShouldThrow_WhenDeliveryDateInPast()
        {
            // Arrange
            var pastDate = DateTime.UtcNow.AddDays(-1);

            // Act + Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.GetActiveAsync(null, null, pastDate, null)
            );
            Assert.Equal("A data de entrega não pode ser anterior à data atual.", ex.Message);
        }

        [Fact]
        public async Task GetActiveAsync_ShouldDefaultPriceOrderToAsc_WhenInvalid()
        {
            // Arrange
            var list = new List<TransportRequest>();
            _repositoryMock.Setup(r => r.GetActiveAsync(null, null, null, "asc"))
                           .ReturnsAsync(list);

            // Act
            var result = await _service.GetActiveAsync(null, null, null, "invalid");

            // Assert
            _repositoryMock.Verify(r => r.GetActiveAsync(null, null, null, "asc"), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnRequest_WhenFound()
        {
            // Arrange
            var request = new TransportRequest { TransportRequestId = 1 };
            _repositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(request);

            // Act
            var result = await _service.GetByIdAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.TransportRequestId);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldThrow_WhenIdIsZeroOrNegative()
        {
            // Act + Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(() => _service.GetByIdAsync(0));
            Assert.Equal("O ID deve ser maior que zero.", ex.Message);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldLogWarning_WhenRequestNotFound()
        {
            // Arrange
            _repositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((TransportRequest?)null);

            // Act
            var result = await _service.GetByIdAsync(1);

            // Assert
            Assert.Null(result);
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Pedido com ID 1 não encontrado.")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()
                ),
                Times.Once
            );
        }
    }
}