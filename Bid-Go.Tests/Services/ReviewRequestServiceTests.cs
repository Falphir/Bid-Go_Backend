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
using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Repositories.Interfaces;
using Bid_Go_Backend.Services.Review;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace Bid_Go_Backend.Tests.Services
{
    public class ReviewRequestServiceTests
    {
        private readonly Mock<IReviewRequestRepository> _mockRepo;
        private readonly Mock<ILogger<ReviewRequestService>> _mockLogger;
        private readonly ReviewRequestService _service;

        public ReviewRequestServiceTests()
        {
            _mockRepo = new Mock<IReviewRequestRepository>(MockBehavior.Strict);
            _mockLogger = new Mock<ILogger<ReviewRequestService>>();
            _service = new ReviewRequestService(_mockRepo.Object, _mockLogger.Object);
        }

        private static ReviewRequestServiceDTO CreateBaseDto(string discriminator)
        {
            return new ReviewRequestServiceDTO
            {
                TimeStamp = DateTime.UtcNow,
                Classification =4.5m,
                DriverId =10,
                CompanyId =20,
                TransportRequestId =100,
                Discriminator = discriminator,
                Punctuality =4,
                Behavior =5,
                ServiceQuality =5,
                ClientSuport =4
            };
        }

        private static TransportRequest CreateTransport(string? statusName)
        {
            var tr = new TransportRequest
            {
                TransportRequestId =100,
                Origin = "O",
                Destination = "D",
                Package = "P",
                Weight =1,
                Volume =1,
                Length =1,
                Width =1,
                Height =1,
                PickupDate = DateTime.UtcNow.AddDays(-2),
                DeliveryDate = DateTime.UtcNow.AddDays(-1),
                Image = "i",
                MaxPrice =10,
                BiddingStartDate = DateTime.UtcNow.AddDays(-10),
                BiddingEndDate = DateTime.UtcNow.AddDays(-9),
                IsAutomaticSelectionEnabled = false,
                CompanyId =20
            };
            if (!string.IsNullOrWhiteSpace(statusName))
            {
                var prop = typeof(TransportRequest).GetProperty("Status", BindingFlags.Public | BindingFlags.Instance);
                var enumType = prop!.PropertyType;
                var value = Enum.Parse(enumType, statusName);
                prop.SetValue(tr, value);
            }
            return tr;
        }

        [Fact]
        public async Task SubmitReviewAsync_ShouldThrow_WhenTransportNotFound()
        {
            var dto = CreateBaseDto("Company");

            _mockRepo.Setup(r => r.GetTransportRequestAsync(dto.TransportRequestId))
                .ReturnsAsync((TransportRequest?)null);

            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.SubmitReviewAsync(dto));

            _mockRepo.Verify(r => r.GetTransportRequestAsync(dto.TransportRequestId), Times.Once);
            _mockRepo.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task SubmitReviewAsync_ShouldThrow_WhenTransportNotCompleted()
        {
            var dto = CreateBaseDto("Company");
            var transport = CreateTransport("Pending");

            _mockRepo.Setup(r => r.GetTransportRequestAsync(dto.TransportRequestId))
                .ReturnsAsync(transport);

            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.SubmitReviewAsync(dto));

            _mockRepo.Verify(r => r.GetTransportRequestAsync(dto.TransportRequestId), Times.Once);
            _mockRepo.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task SubmitReviewAsync_ShouldThrow_WhenDiscriminatorInvalid_OnExistsCheck()
        {
            var dto = CreateBaseDto("Unknown");
            var transport = CreateTransport("Completed");

            _mockRepo.Setup(r => r.GetTransportRequestAsync(dto.TransportRequestId))
                .ReturnsAsync(transport);

            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.SubmitReviewAsync(dto));

            _mockRepo.Verify(r => r.GetTransportRequestAsync(dto.TransportRequestId), Times.Once);
            _mockRepo.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task SubmitReviewAsync_ShouldThrow_WhenCompanyReviewAlreadyExists()
        {
            var dto = CreateBaseDto("Company");
            var transport = CreateTransport("Completed");

            _mockRepo.Setup(r => r.GetTransportRequestAsync(dto.TransportRequestId))
                .ReturnsAsync(transport);
            _mockRepo.Setup(r => r.CompanyReviewExistsAsync(dto.TransportRequestId, dto.CompanyId))
                .ReturnsAsync(true);

            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.SubmitReviewAsync(dto));

            _mockRepo.Verify(r => r.GetTransportRequestAsync(dto.TransportRequestId), Times.Once);
            _mockRepo.Verify(r => r.CompanyReviewExistsAsync(dto.TransportRequestId, dto.CompanyId), Times.Once);
            _mockRepo.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task SubmitReviewAsync_ShouldThrow_WhenDriverReviewAlreadyExists()
        {
            var dto = CreateBaseDto("Driver");
            var transport = CreateTransport("Completed");

            _mockRepo.Setup(r => r.GetTransportRequestAsync(dto.TransportRequestId))
                .ReturnsAsync(transport);
            _mockRepo.Setup(r => r.DriverReviewExistsAsync(dto.TransportRequestId, dto.DriverId))
                .ReturnsAsync(true);

            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.SubmitReviewAsync(dto));

            _mockRepo.Verify(r => r.GetTransportRequestAsync(dto.TransportRequestId), Times.Once);
            _mockRepo.Verify(r => r.DriverReviewExistsAsync(dto.TransportRequestId, dto.DriverId), Times.Once);
            _mockRepo.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(6)]
        public async Task SubmitReviewAsync_ShouldThrow_WhenClassificationOutOfRange(decimal classification)
        {
            var dto = CreateBaseDto("Company");
            dto.Classification = classification;
            var transport = CreateTransport("Completed");

            _mockRepo.Setup(r => r.GetTransportRequestAsync(dto.TransportRequestId))
                .ReturnsAsync(transport);
            _mockRepo.Setup(r => r.CompanyReviewExistsAsync(dto.TransportRequestId, dto.CompanyId))
                .ReturnsAsync(false);

            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => _service.SubmitReviewAsync(dto));

            _mockRepo.Verify(r => r.GetTransportRequestAsync(dto.TransportRequestId), Times.Once);
            _mockRepo.Verify(r => r.CompanyReviewExistsAsync(dto.TransportRequestId, dto.CompanyId), Times.Once);
            _mockRepo.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task SubmitReviewAsync_ShouldAddCompanyReview_WhenValid()
        {
            var dto = CreateBaseDto("Company");
            var transport = CreateTransport("Completed");
            Bid_Go_Backend.Data.Models.Review? saved = null;

            _mockRepo.Setup(r => r.GetTransportRequestAsync(dto.TransportRequestId))
                .ReturnsAsync(transport);
            _mockRepo.Setup(r => r.CompanyReviewExistsAsync(dto.TransportRequestId, dto.CompanyId))
                .ReturnsAsync(false);
            _mockRepo.Setup(r => r.AddReviewAsync(It.IsAny<Bid_Go_Backend.Data.Models.Review>()))
                .Callback<Bid_Go_Backend.Data.Models.Review>(r => saved = r)
                .Returns(Task.CompletedTask);

            var result = await _service.SubmitReviewAsync(dto);

            Assert.True(result);
            Assert.NotNull(saved);
            Assert.IsType<ReviewCompany>(saved);
            var companyReview = Assert.IsType<ReviewCompany>(saved);
            Assert.Equal(dto.ServiceQuality, companyReview.ServiceQuality);
            Assert.Equal(dto.ClientSuport, companyReview.ClientSuport);
            Assert.Equal(dto.TransportRequestId, companyReview.TransportRequestId);
            Assert.Equal(dto.CompanyId, companyReview.CompanyId);
            Assert.Equal(dto.DriverId, companyReview.DriverId);
            Assert.Equal(dto.Classification, companyReview.Classification);

            _mockRepo.Verify(r => r.GetTransportRequestAsync(dto.TransportRequestId), Times.Once);
            _mockRepo.Verify(r => r.CompanyReviewExistsAsync(dto.TransportRequestId, dto.CompanyId), Times.Once);
            _mockRepo.Verify(r => r.AddReviewAsync(It.IsAny<Bid_Go_Backend.Data.Models.Review>()), Times.Once);
            _mockRepo.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task SubmitReviewAsync_ShouldAddDriverReview_WhenValid()
        {
            var dto = CreateBaseDto("Driver");
            var transport = CreateTransport("Completed");
            Bid_Go_Backend.Data.Models.Review? saved = null;

            _mockRepo.Setup(r => r.GetTransportRequestAsync(dto.TransportRequestId))
                .ReturnsAsync(transport);
            _mockRepo.Setup(r => r.DriverReviewExistsAsync(dto.TransportRequestId, dto.DriverId))
                .ReturnsAsync(false);
            _mockRepo.Setup(r => r.AddReviewAsync(It.IsAny<Bid_Go_Backend.Data.Models.Review>()))
                .Callback<Bid_Go_Backend.Data.Models.Review>(r => saved = r)
                .Returns(Task.CompletedTask);

            var result = await _service.SubmitReviewAsync(dto);

            Assert.True(result);
            Assert.NotNull(saved);
            Assert.IsType<ReviewDriver>(saved);
            var driverReview = Assert.IsType<ReviewDriver>(saved);
            Assert.Equal(dto.Punctuality, driverReview.Punctuality);
            Assert.Equal(dto.Behavior, driverReview.Behavior);
            Assert.Equal(dto.TransportRequestId, driverReview.TransportRequestId);
            Assert.Equal(dto.CompanyId, driverReview.CompanyId);
            Assert.Equal(dto.DriverId, driverReview.DriverId);
            Assert.Equal(dto.Classification, driverReview.Classification);

            _mockRepo.Verify(r => r.GetTransportRequestAsync(dto.TransportRequestId), Times.Once);
            _mockRepo.Verify(r => r.DriverReviewExistsAsync(dto.TransportRequestId, dto.DriverId), Times.Once);
            _mockRepo.Verify(r => r.AddReviewAsync(It.IsAny<Bid_Go_Backend.Data.Models.Review>()), Times.Once);
            _mockRepo.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task GetReviewByServiceIdAsync_ShouldReturnData_FromRepository()
        {
            var list = new List<ReviewByServiceDTO>
            {
                new ReviewByServiceDTO
                {
                    TimeStamp = DateTime.UtcNow,
                    Classification =4.5m,
                    Name = "Alice",
                    Punctuality =5,
                    Behavior =5,
                    ServiceQuality = null,
                    ClientSuport = null
                },
                new ReviewByServiceDTO
                {
                    TimeStamp = DateTime.UtcNow,
                    Classification =4.0m,
                    Name = "Bob",
                    Punctuality = null,
                    Behavior = null,
                    ServiceQuality =4,
                    ClientSuport =4
                }
            };

            _mockRepo.Setup(r => r.GetReviewByServiceIdAsync(100))
                .ReturnsAsync(list);

            var result = await _service.GetReviewByServiceIdAsync(100);

            Assert.NotNull(result);
            Assert.Collection(result,
                item => Assert.Equal("Alice", item.Name),
                item => Assert.Equal("Bob", item.Name));

            _mockRepo.Verify(r => r.GetReviewByServiceIdAsync(100), Times.Once);
            _mockRepo.VerifyNoOtherCalls();
        }
    }
}
