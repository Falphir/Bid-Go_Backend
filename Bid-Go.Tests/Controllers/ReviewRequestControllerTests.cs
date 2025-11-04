using Bid_Go_Backend.Controllers;
using Bid_Go_Backend.Data.Models.DTOs;
using Bid_Go_Backend.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Bid_Go.Tests.Controllers
{
    public class ReviewRequestControllerTests
    {
        private readonly Mock<IReviewRequestService> _mockRepo;
        private readonly ReviewRequestController _controller;

        public ReviewRequestControllerTests()
        {
            _mockRepo = new Mock<IReviewRequestService>();
            _controller = new ReviewRequestController(_mockRepo.Object);
        }

        [Fact]
        public async Task SubmitReview_ShouldReturnOk_WhenRepoReturnsTrue()
        {
            // Arrange
            var dto = new ReviewRequestServiceDTO
            {
                Discriminator = "driver",
                Classification = 4.5m,
                DriverId = 10,
                CompanyId = 1,
                TransportRequestId = 100,
                Punctuality = 5,
                Behavior = 5,
                ServiceQuality = 4,
                ClientSuport = 4
            };

            _mockRepo
                .Setup(r => r.SubmitReviewAsync(dto))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.SubmitReview(dto);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(StatusCodes.Status200OK, ok.StatusCode);
        }

        [Fact]
        public async Task SubmitReview_ShouldReturnBadRequest_WhenRepoReturnsFalse()
        {
            // Arrange
            var dto = new ReviewRequestServiceDTO
            {
                Discriminator = "driver",
                Classification = 3m,
                DriverId = 10,
                CompanyId = 1,
                TransportRequestId = 100,
                Punctuality = 3,
                Behavior = 3,
                ServiceQuality = 3,
                ClientSuport = 3
            };

            _mockRepo
                .Setup(r => r.SubmitReviewAsync(dto))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.SubmitReview(dto);

            // Assert
            var bad = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, bad.StatusCode);
        }

        [Fact]
        public async Task SubmitReview_ShouldReturnBadRequest_WhenRepoThrowsInvalidOperation()
        {
            // Arrange
            var dto = new ReviewRequestServiceDTO
            {
                Discriminator = "driver",
                Classification = 4m,
                DriverId = 10,
                CompanyId = 1,
                TransportRequestId = 100,
                Punctuality = 4,
                Behavior = 4,
                ServiceQuality = 4,
                ClientSuport = 4
            };

            _mockRepo
                .Setup(r => r.SubmitReviewAsync(dto))
                .ThrowsAsync(new InvalidOperationException("review já existe"));

            // Act
            var result = await _controller.SubmitReview(dto);

            // Assert
            var bad = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, bad.StatusCode);
        }

        [Fact]
        public async Task SubmitReview_ShouldReturnBadRequest_WhenRepoThrowsArgumentOutOfRange()
        {
            // Arrange
            var dto = new ReviewRequestServiceDTO
            {
                Discriminator = "driver",
                Classification = 10m, // inválido
                DriverId = 10,
                CompanyId = 1,
                TransportRequestId = 100,
                Punctuality = 10,
                Behavior = 10,
                ServiceQuality = 10,
                ClientSuport = 10
            };

            _mockRepo
                .Setup(r => r.SubmitReviewAsync(dto))
                .ThrowsAsync(new ArgumentOutOfRangeException("Classification", "Valor inválido"));

            // Act
            var result = await _controller.SubmitReview(dto);

            // Assert
            var bad = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, bad.StatusCode);
        }

        [Fact]
        public async Task SubmitReview_ShouldReturn500_WhenRepoThrowsUnexpected()
        {
            // Arrange
            var dto = new ReviewRequestServiceDTO
            {
                Discriminator = "driver",
                Classification = 4m,
                DriverId = 10,
                CompanyId = 1,
                TransportRequestId = 100,
                Punctuality = 4,
                Behavior = 4,
                ServiceQuality = 4,
                ClientSuport = 4
            };

            _mockRepo
                .Setup(r => r.SubmitReviewAsync(dto))
                .ThrowsAsync(new Exception("boom"));

            // Act
            var result = await _controller.SubmitReview(dto);

            // Assert
            var obj = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, obj.StatusCode);
        }

        [Fact]
        public async Task GetReviewsByService_ShouldReturnNotFound_WhenRepoThrowsInvalidOperation()
        {
            // Arrange
            int transportRequestId = 99;

            _mockRepo
                .Setup(r => r.GetReviewByServiceIdAsync(transportRequestId))
                .ThrowsAsync(new InvalidOperationException("não encontrado"));

            // Act
            var result = await _controller.GetReviewsByService(transportRequestId);

            // Assert
            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal(StatusCodes.Status404NotFound, notFound.StatusCode);
        }

        [Fact]
        public async Task GetReviewsByService_ShouldReturn500_WhenRepoThrowsUnexpected()
        {
            // Arrange
            int transportRequestId = 99;

            _mockRepo
                .Setup(r => r.GetReviewByServiceIdAsync(transportRequestId))
                .ThrowsAsync(new Exception("erro qualquer"));

            // Act
            var result = await _controller.GetReviewsByService(transportRequestId);

            // Assert
            var obj = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, obj.StatusCode);
        }
    }
}
