using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.DTOs;
using Bid_Go_Backend.Data.Models.Enums;
using Bid_Go_Backend.Data.Repositories.Interfaces;
using Bid_Go_Backend.Services;
using Moq;
using Xunit;

namespace Bid_Go_Backend.Tests.Services
{
    public class TransportRequestServiceTests
    {
        private readonly Mock<ITransportRequestRepository> _repositoryMock;
        private readonly TransportRequestService _service;

        public TransportRequestServiceTests()
        {
            _repositoryMock = new Mock<ITransportRequestRepository>();
            _service = new TransportRequestService(_repositoryMock.Object);
        }

        
        [Fact]
        public async Task CreateAsync_ShouldThrow_WhenPickupAfterDelivery()
        {
            var dto = new CreateTransportRequestDTO
            {
                PickupDate = DateTime.UtcNow.AddDays(2),
                DeliveryDate = DateTime.UtcNow.AddDays(1),
                Image = "image.jpg",
                Weight = 10,
                Volume = 10,
                MaxPrice = 50
            };

            await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateAsync(dto));
        }

        [Fact]
        public async Task CreateAsync_ShouldThrow_WhenImageIsMissing()
        {
            var dto = new CreateTransportRequestDTO
            {
                PickupDate = DateTime.UtcNow,
                DeliveryDate = DateTime.UtcNow.AddDays(1),
                Image = "",
                Weight = 10,
                Volume = 10,
                MaxPrice = 50
            };

            await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateAsync(dto));
        }

        [Fact]
        public async Task CreateAsync_ShouldReturnCreatedRequest_WhenValid()
        {
            var dto = new CreateTransportRequestDTO
            {
                Origin = "Lisboa",
                Destination = "Porto",
                Package = "Caixa",
                PickupDate = DateTime.UtcNow,
                DeliveryDate = DateTime.UtcNow.AddDays(2),
                Image = "img.jpg",
                Weight = 10,
                Volume = 10,
                Length = 50,
                Width = 30,
                Height = 40,
                MaxPrice = 60,
                CompanyId = 1
            };

            var expected = new TransportRequest { TransportRequestId = 1, Origin = dto.Origin };

            _repositoryMock
                .Setup(r => r.CreateAsync(It.IsAny<TransportRequest>()))
                .ReturnsAsync(expected);

            var result = await _service.CreateAsync(dto);

            Assert.NotNull(result);
            Assert.Equal(expected.TransportRequestId, result.TransportRequestId);
            Assert.Equal("Lisboa", result.Origin);
            _repositoryMock.Verify(r => r.CreateAsync(It.IsAny<TransportRequest>()), Times.Once);
        }

     
        [Fact]
        public async Task UpdateAsync_ShouldThrow_WhenRequestNotFound()
        {
            _repositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
                .ReturnsAsync((TransportRequest?)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.UpdateAsync(1, new UpdateTransportRequestDTO()));
        }

        [Fact]
        public async Task UpdateAsync_ShouldThrow_WhenStatusNotDraft()
        {
            var existing = new TransportRequest { Status = ERequestStatus.Active };
            _repositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);

            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.UpdateAsync(1, new UpdateTransportRequestDTO()));
        }

   
        [Fact]
        public async Task DeleteAsync_ShouldThrow_WhenRequestNotFound()
        {
            _repositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
                .ReturnsAsync((TransportRequest?)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.DeleteAsync(1));
        }

        [Fact]
        public async Task DeleteAsync_ShouldThrow_WhenStatusNotActive()
        {
            var existing = new TransportRequest { Status = ERequestStatus.Draft };
            _repositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);

            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.DeleteAsync(1));
        }

        [Fact]
        public async Task DeleteAsync_ShouldReturnTrue_WhenValid()
        {
            var existing = new TransportRequest { Status = ERequestStatus.Active };
            _repositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);
            _repositoryMock.Setup(r => r.DeleteAsync(1)).ReturnsAsync(true);

            var result = await _service.DeleteAsync(1);

            Assert.True(result);
            _repositoryMock.Verify(r => r.DeleteAsync(1), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnRequest()
        {
            var expected = new TransportRequest { TransportRequestId = 5 };
            _repositoryMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(expected);

            var result = await _service.GetByIdAsync(5);

            Assert.NotNull(result);
            Assert.Equal(5, result.TransportRequestId);
        }

        [Fact]
        public async Task GetByCompanyAsync_ShouldReturnList()
        {
            var list = new List<TransportRequest>
            {
                new TransportRequest { TransportRequestId = 1, CompanyId = 2 },
                new TransportRequest { TransportRequestId = 2, CompanyId = 2 }
            };

            _repositoryMock.Setup(r => r.GetAllByCompanyAsync(2)).ReturnsAsync(list);

            var result = await _service.GetByCompanyAsync(2);

            Assert.Equal(2, result.Count);
            Assert.All(result, r => Assert.Equal(2, r.CompanyId));
        }

        [Fact]
        public async Task UpdateAsync_ShouldUpdateOnlyProvidedFields()
        {
            // Arrange
            var existing = new TransportRequest
            {
                TransportRequestId = 1,
                Origin = "Lisboa",
                Destination = "Porto",
                Package = "Caixa antiga",
                Weight = 10,
                Volume = 15,
                Length = 50,
                Width = 30,
                Height = 25,
                Image = "img1.jpg",
                PickupDate = new DateTime(2025, 11, 1),
                DeliveryDate = new DateTime(2025, 11, 10),
                MaxPrice = 100,
                Status = ERequestStatus.Draft
            };

            var dto = new UpdateTransportRequestDTO
            {
                Destination = "Braga",
                Weight = 20,
                MaxPrice = 200
                // Nota: não preenchemos os outros campos para ver se mantêm o valor anterior
            };

            _repositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);
            _repositoryMock.Setup(r => r.UpdateAsync(1, It.IsAny<TransportRequest>()))
                .ReturnsAsync((int id, TransportRequest req) => req);

            // Act
            var result = await _service.UpdateAsync(1, dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Lisboa", result.Origin); // manteve valor original
            Assert.Equal("Braga", result.Destination); // alterado
            Assert.Equal(20, result.Weight); // alterado
            Assert.Equal(15, result.Volume); // manteve original
            Assert.Equal(200, result.MaxPrice); // alterado

            _repositoryMock.Verify(r => r.UpdateAsync(1, It.IsAny<TransportRequest>()), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_ShouldThrow_WhenPickupAfterDelivery()
        {
            // Arrange
            var existing = new TransportRequest { Status = ERequestStatus.Draft };
            _repositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);

            var dto = new UpdateTransportRequestDTO
            {
                PickupDate = DateTime.UtcNow.AddDays(5),
                DeliveryDate = DateTime.UtcNow.AddDays(1)
            };

            // Act + Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(() => _service.UpdateAsync(1, dto));
            Assert.Equal("A data de recolha deve ser anterior à data de entrega.", ex.Message);
        }

        [Fact]
        public async Task UpdateAsync_ShouldThrow_WhenMaxPriceBelowMinimum()
        {
            // Arrange
            var existing = new TransportRequest { Status = ERequestStatus.Draft };
            _repositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);

            var dto = new UpdateTransportRequestDTO
            {
                MaxPrice = 10 
            };

            // Act + Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(() => _service.UpdateAsync(1, dto));
            Assert.Equal("O preço deve ser igual ou superior a 20.", ex.Message);
        }

    }
}
