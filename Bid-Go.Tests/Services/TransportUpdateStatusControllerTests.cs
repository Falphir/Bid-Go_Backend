using Bid_Go_Backend.Controllers;
using Bid_Go_Backend.Data;
using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.DTOs;
using Bid_Go_Backend.Data.Models.Enums;
using Bid_Go_Backend.Data.Repositories.Interfaces;
using Bid_Go_Backend.Repositories.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
using Moq;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Bid_Go.Tests.Controllers
{
    public class TransportUpdateStatusControllerTests
    {
        private readonly Mock<ITransportUpdateStatus> _mockRepo;
        private readonly BidGoDbContext _ctx;
        private readonly TransportUpdateStatusController _controller;

        public TransportUpdateStatusControllerTests()
        {
            _mockRepo = new Mock<ITransportUpdateStatus>();

            var options = new DbContextOptionsBuilder<BidGoDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _ctx = new BidGoDbContext(options);
            _ctx.Database.EnsureCreated();

            _controller = new TransportUpdateStatusController(_mockRepo.Object);
        }

        [Fact]
        public async Task UpdateRequestStatus_ShouldReturnNotFound_WhenTransportRequestDoesNotExist()
        {
            //Arrange

            var requestStatus = new RequestStatusDTO
            {
                Status = ERequestStatus.Active
            };

            int requestID = 1;

            //Act
            var result = await _controller.UpdateRequestStatus(requestID, 1, requestStatus);

            //Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task UpdateRequestStatus_ShouldReturnOK_WhenSuccessful()
        {
            // Arrange
            var companyId = 1;
            var requestId = 1;

            var requestStatus = new RequestStatusDTO
            {
                Status = ERequestStatus.Completed
            };

            var responseDto = new TransportRequestResponseDTO
            {
                Origin = "Lisboa",
                Destination = "Coimbra",
                Package = "Envelope",
                PickupDate = DateTime.Now,
                DeliveryDate = DateTime.Now.AddDays(1),
                Weight = 2,
                Volume = 1,
                Length = 0.5m,
                Width = 0.5m,
                Height = 0.5m,
                Image = "image.jpg",
                MaxPrice = 50,
                Status = ERequestStatus.Completed
            };

            _mockRepo
                .Setup(r => r.UpdateRequestStatusAsync(requestId, companyId, requestStatus.Status))
                .ReturnsAsync(responseDto); // 👈 devolve o DTO correto

            // Act
            var result = await _controller.UpdateRequestStatus(requestId, companyId, requestStatus);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
        }


        [Fact]
        public async Task UpdateRequestStatus_ShouldReturnBadRequest_WhenRepoThrowsInvalidOperation()
        {
            // Arrange
            var dto = new RequestStatusDTO { Status = ERequestStatus.Active };
            int requestID = 1, companyID = 1;

            _mockRepo
                .Setup(r => r.UpdateRequestStatusAsync(requestID, companyID, dto.Status))
                .ThrowsAsync(new InvalidOperationException("estado inválido"));

            // Act
            var result = await _controller.UpdateRequestStatus(requestID, companyID, dto);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, badRequest.StatusCode);
        }

        [Fact]
        public async Task UpdateRequestStatus_ShouldReturn500_WhenRepoThrowsUnexpectedException()
        {
            // Arrange
            var dto = new RequestStatusDTO { Status = ERequestStatus.Active };
            int requestID = 1, companyID = 1;

            _mockRepo
                .Setup(r => r.UpdateRequestStatusAsync(requestID, companyID, dto.Status))
                .ThrowsAsync(new Exception("boom"));

            // Act
            var result = await _controller.UpdateRequestStatus(requestID, companyID, dto);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, objectResult.StatusCode);
        }

        [Fact]
        public async Task CancelRequestStatus_ShouldReturnNotFound_WhenTransportRequestDoesNotExist()
        {
            // Arrange
            int requestID = 10;
            int companyID = 1;

            _mockRepo
                .Setup(r => r.UpdateRequestStatusAsync(requestID, companyID, ERequestStatus.Canceled))
                .ReturnsAsync((TransportRequestResponseDTO?)null);

            // Act
            var result = await _controller.CancelRequestStatus(requestID, companyID);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task CancelRequestStatus_ShouldReturnOK_WhenSuccessful()
        {
            // Arrange
            int requestID = 10;
            int companyID = 1;

            var responseDto = new TransportRequestResponseDTO
            {
                Origin = "Lisboa",
                Destination = "Porto",
                Package = "Caixa",
                PickupDate = DateTime.Now,
                DeliveryDate = DateTime.Now.AddDays(1),
                Weight = 10,
                Volume = 5,
                Length = 2,
                Width = 1,
                Height = 1,
                Image = "image.jpg",
                MaxPrice = 100,
                Status = ERequestStatus.Canceled
            };

            _mockRepo
                .Setup(r => r.UpdateRequestStatusAsync(requestID, companyID, ERequestStatus.Canceled))
                .ReturnsAsync(responseDto); // 👈 devolve um DTO válido

            // Act
            var result = await _controller.CancelRequestStatus(requestID, companyID);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
        }


        [Fact]
        public async Task CancelRequestStatus_ShouldReturnBadRequest_WhenRepoThrowsInvalidOperation()
        {
            // Arrange
            int requestID = 10;
            int companyID = 1;

            _mockRepo
                .Setup(r => r.UpdateRequestStatusAsync(requestID, companyID, ERequestStatus.Canceled))
                .ThrowsAsync(new InvalidOperationException("não pode cancelar"));

            // Act
            var result = await _controller.CancelRequestStatus(requestID, companyID);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, badRequest.StatusCode);
        }

        [Fact]
        public async Task CancelRequestStatus_ShouldReturn500_WhenRepoThrowsUnexpectedException()
        {
            // Arrange
            int requestID = 10;
            int companyID = 1;

            _mockRepo
                .Setup(r => r.UpdateRequestStatusAsync(requestID, companyID, ERequestStatus.Canceled))
                .ThrowsAsync(new Exception("erro qualquer"));

            // Act
            var result = await _controller.CancelRequestStatus(requestID, companyID);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, objectResult.StatusCode);
        }
    }
}
