using Bid_Go_Backend.Controllers;
using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.DTOs;
using Bid_Go_Backend.Data.Repositories.Interfaces;

using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Bid_Go_Tests.Controllers
{
    public class TransportRequestsPageControllerTests
    {
        private readonly Mock<ITransportRequestsPageRepository> _mockRepo;
        private readonly TransportRequestsPageController _controller;

        public TransportRequestsPageControllerTests()
        {
            _mockRepo = new Mock<ITransportRequestsPageRepository>();
            _controller = new TransportRequestsPageController(_mockRepo.Object);
        }

     
        [Fact]
        public async Task GetActive_ShouldReturnMessage_WhenNoRequestsFound()
        {
            // Arrange
            _mockRepo.Setup(r => r.GetActiveAsync(null, null, null, null))
                     .ReturnsAsync(new List<TransportRequest>());

            // Act
            var result = await _controller.GetActive(null, null, null, null);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);

          
            var messageProperty = okResult.Value.GetType().GetProperty("message");
            var messageValue = messageProperty?.GetValue(okResult.Value, null);

            Assert.Equal("Não existem pedidos ativos no momento.", messageValue);
        }


        [Fact]
        public async Task GetActive_ShouldReturnOk_WithListOfDTOs()
        {
            // Arrange
            var requests = new List<TransportRequest>
            {
                new TransportRequest
                {
                    Origin = "Lisboa",
                    Destination = "Porto",
                    Package = "Caixa",
                    PickupDate = DateTime.UtcNow,
                    DeliveryDate = DateTime.UtcNow.AddDays(2),
                    Image = "img1.jpg",
                    MaxPrice = 100
                },
                new TransportRequest
                {
                    Origin = "Madrid",
                    Destination = "Barcelona",
                    Package = "Envelope",
                    PickupDate = DateTime.UtcNow.AddDays(1),
                    DeliveryDate = DateTime.UtcNow.AddDays(3),
                    Image = "img2.jpg",
                    MaxPrice = 50
                }
            };

            _mockRepo.Setup(r => r.GetActiveAsync(null, null, null, null))
                     .ReturnsAsync(requests);

            // Act
            var result = await _controller.GetActive(null, null, null, null);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var dtoList = Assert.IsAssignableFrom<IEnumerable<TransportRequestsPageDTO>>(okResult.Value);
            Assert.Equal(2, dtoList.Count());
            Assert.Equal("Lisboa", dtoList.First().Origin);
        }

  

        [Fact]
        public async Task GetById_ShouldReturnNotFound_WhenTransportRequestDoesNotExist()
        {
            // Arrange
            _mockRepo.Setup(r => r.GetByIdAsync(1))
                     .ReturnsAsync((TransportRequest)null);

            // Act
            var result = await _controller.GetById(1);

            // Assert
            var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Equal("Pedido de transporte não existe", notFound.Value);
        }

        [Fact]
        public async Task GetById_ShouldReturnOk_WithResponseDTO()
        {
            // Arrange
            var request = new TransportRequest
            {
                Origin = "Lisboa",
                Destination = "Porto",
                Package = "Caixa",
                PickupDate = DateTime.UtcNow,
                DeliveryDate = DateTime.UtcNow.AddDays(1),
                Weight = 10,
                Volume = 5,
                Length = 1.2m,
                Width = 0.5m,
                Height = 0.4m,
                MaxPrice = 120,
                Image = "foto.jpg"
            };

            _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(request);

            // Act
            var result = await _controller.GetById(1);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var dto = Assert.IsType<TransportRequestResponseDTO>(ok.Value);
            Assert.Equal("Lisboa", dto.Origin);
            Assert.Equal(10, dto.Weight);
            Assert.Equal(120, dto.MaxPrice);
        }

        [Fact]
        public async Task GetById_ShouldReturnStatus500_WhenExceptionThrown()
        {
            // Arrange
            _mockRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
                     .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.GetById(1);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, objectResult.StatusCode);

            var messageProperty = objectResult.Value.GetType().GetProperty("message");
            var messageValue = messageProperty?.GetValue(objectResult.Value, null) as string;

            Assert.NotNull(messageValue);
            Assert.Contains("Erro inesperado", messageValue);
        }

    }
}
