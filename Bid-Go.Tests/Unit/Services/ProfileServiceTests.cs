using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.DTOs;
using Bid_Go_Backend.Services.Profile;
using Moq;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Xunit;
using Bid_Go_Backend.Services.Interfaces;
using Bid_Go_Backend.Repositories.Interfaces;

namespace Bid_Go.Tests.Unit.Services
{
    /// <summary>
    /// Unit tests for ProfileService with driver/company operations and validations.
    /// </summary>
    public class ProfileServiceTests
    {
        private readonly Mock<IProfileRepository> _mockRepo;
        private readonly Mock<ICloudflareR2Service> _mockCloudflare;
        private readonly ProfileService _service;

        public ProfileServiceTests()
        {
            _mockRepo = new Mock<IProfileRepository>();
            // Use a simple mock for the interface
            _mockCloudflare = new Mock<ICloudflareR2Service>();
            _service = new ProfileService(_mockRepo.Object, _mockCloudflare.Object);
        }

      
        [Fact]
        public async Task GetProfileAsync_ReturnsUser_WhenActive()
        {
            // Arrange
            var user = new Driver { Id =1, IsActive = true };
            _mockRepo.Setup(r => r.GetUserByIdAsync(1)).ReturnsAsync(user);

            // Act
            var result = await _service.GetProfileAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
        }

        [Fact]
        public async Task GetProfileAsync_ReturnsNull_WhenNotFound()
        {
            // Arrange
            _mockRepo.Setup(r => r.GetUserByIdAsync(1)).ReturnsAsync((User?)null);

            // Act
            var result = await _service.GetProfileAsync(1);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetProfileAsync_ThrowsException_WhenInactive()
        {
            // Arrange
            var user = new Driver { Id = 1, IsActive = false };
            _mockRepo.Setup(r => r.GetUserByIdAsync(1)).ReturnsAsync(user);

            // Act
            // Assert
            await Assert.ThrowsAsync<Exception>(() => _service.GetProfileAsync(1));
        }

     
        [Fact]
        public async Task UpdateDriverProfileAsync_UpdatesDriverSuccessfully()
        {
            // Arrange
            var driver = new Driver
            {
                Id = 1,
                IsActive = true,
                Name = "Old",
                Email = "old@email.com"
            };

            var dto = new DriverProfileUpdateDTO
            {
                Name = "New",
                Email = "new@email.com"
            };

            _mockRepo.Setup(r => r.GetUserByIdAsync(1)).ReturnsAsync(driver);
            _mockRepo
                .Setup(r => r.UpdateDriverAsync(It.IsAny<Driver>()))
                .ReturnsAsync(true);

            // Act
            var result = await _service.UpdateDriverProfileAsync(1, dto);

            // Assert
            Assert.True(result);
            _mockRepo.Verify(r => r.UpdateDriverAsync(It.IsAny<Driver>()), Times.Once);
        }

        [Fact]
        public async Task UpdateDriverProfileAsync_Throws_WhenDriverHasNoChanges()
        {
            // Arrange
            var driver = new Driver { Id = 1, IsActive = true, Name = "Same" };
            var dto = new DriverProfileUpdateDTO { Name = "Same" };

            _mockRepo.Setup(r => r.GetUserByIdAsync(1)).ReturnsAsync(driver);

            // Act
            // Assert
            await Assert.ThrowsAsync<Exception>(() => _service.UpdateDriverProfileAsync(1, dto));
        }

        [Fact]
        public async Task UpdateDriverProfileAsync_Throws_WhenInvalidDriverDto()
        {
            // Arrange
            var driver = new Driver { Id = 1, IsActive = true };
            _mockRepo.Setup(r => r.GetUserByIdAsync(1)).ReturnsAsync(driver);

            // Act
            // Assert
            await Assert.ThrowsAsync<Exception>(() => _service.UpdateDriverProfileAsync(1, new DriverProfileUpdateDTO()));
        }

    
        [Fact]
        public async Task UpdateCompanyProfileAsync_UpdatesCompanySuccessfully()
        {
            // Arrange
            var company = new Company
            {
                Id = 1,
                IsActive = true,
                Name = "OldCo",
                CompanyName = "Old Company"
            };

            var dto = new CompanyProfileDTO
            {
                Name = "NewCo",
                CompanyName = "New Company"
            };

            _mockRepo.Setup(r => r.GetUserByIdAsync(1)).ReturnsAsync(company);
            _mockRepo
                .Setup(r => r.UpdateCompanyAsync(It.IsAny<Company>()))
                .ReturnsAsync(true);


            // Act
            var result = await _service.UpdateCompanyProfileAsync(1, dto);

            // Assert
            Assert.True(result);
            _mockRepo.Verify(r => r.UpdateCompanyAsync(It.IsAny<Company>()), Times.Once);
        }

        [Fact]
        public async Task UpdateCompanyProfileAsync_Throws_WhenInvalidCompanyDto()
        {
            // Arrange
            var company = new Company { Id = 1, IsActive = true };
            _mockRepo.Setup(r => r.GetUserByIdAsync(1)).ReturnsAsync(company);

            // Act
            // Assert
            await Assert.ThrowsAsync<Exception>(() => _service.UpdateCompanyProfileAsync(1, new CompanyProfileDTO()));
        }

        [Fact]
        public async Task UpdateCompanyProfileAsync_Throws_WhenUserInactive()
        {
            // Arrange
            var user = new Company { Id = 1, IsActive = false };
            _mockRepo.Setup(r => r.GetUserByIdAsync(1)).ReturnsAsync(user);

            // Act
            // Assert
            await Assert.ThrowsAsync<Exception>(() => _service.UpdateCompanyProfileAsync(1, new CompanyProfileDTO()));
        }

      
        [Fact]
        public async Task ChangePasswordAsync_UpdatesPassword_WhenValid()
        {
            // Arrange
            var hashed = BCrypt.Net.BCrypt.HashPassword("oldpass");
            var user = new Driver { Id = 1, IsActive = true, Password = hashed };

            _mockRepo.Setup(r => r.GetUserByIdAsync(1)).ReturnsAsync(user);
            _mockRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            // Act
            var result = await _service.ChangePasswordAsync(1, "oldpass", "newpass");

            // Assert
            Assert.True(result);
            _mockRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task ChangePasswordAsync_Throws_WhenCurrentPasswordIncorrect()
        {
            // Arrange
            var hashed = BCrypt.Net.BCrypt.HashPassword("correct");
            var user = new Company { Id = 1, IsActive = true, Password = hashed };

            _mockRepo.Setup(r => r.GetUserByIdAsync(1)).ReturnsAsync(user);

            // Act
            // Assert
            await Assert.ThrowsAsync<Exception>(() => _service.ChangePasswordAsync(1, "wrong", "new"));
        }

        [Fact]
        public async Task DeactivateUserAsync_Throws_WhenUserNotFound()
        {
            // Arrange
            _mockRepo.Setup(r => r.GetUserByIdAsync(1)).ReturnsAsync((User?)null);

            // Act
            // Assert
            await Assert.ThrowsAsync<Exception>(() => _service.DeactivateUserAsync(1));
        }

        [Fact]
        public async Task DeactivateUserAsync_Throws_WhenAlreadyInactive()
        {
            // Arrange
            var user = new Company { Id = 1, IsActive = false };
            _mockRepo.Setup(r => r.GetUserByIdAsync(1)).ReturnsAsync(user);

            // Act
            // Assert
            await Assert.ThrowsAsync<Exception>(() => _service.DeactivateUserAsync(1));
        }

        [Fact]
        public async Task DeactivateUserAsync_DeactivatesDriver_WhenNoActiveBids()
        {
            // Arrange
            var driver = new Driver { Id = 1, IsActive = true };

            _mockRepo.Setup(r => r.GetUserByIdAsync(1)).ReturnsAsync(driver);
            _mockRepo.Setup(r => r.HasActiveBidsAsync(1)).ReturnsAsync(false);
            _mockRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            // Act
            var result = await _service.DeactivateUserAsync(1);

            // Assert
            Assert.True(result);
            Assert.False(driver.IsActive);
        }

        [Fact]
        public async Task DeactivateUserAsync_Throws_WhenDriverHasActiveBids()
        {
            // Arrange
            var driver = new Driver { Id = 1, IsActive = true };

            _mockRepo.Setup(r => r.GetUserByIdAsync(1)).ReturnsAsync(driver);
            _mockRepo.Setup(r => r.HasActiveBidsAsync(1)).ReturnsAsync(true);

            // Act
            // Assert
            await Assert.ThrowsAsync<Exception>(() => _service.DeactivateUserAsync(1));
        }

        [Fact]
        public async Task DeactivateUserAsync_Throws_WhenCompanyHasActiveRequests()
        {
            // Arrange
            var company = new Company { Id = 1, IsActive = true };

            _mockRepo.Setup(r => r.GetUserByIdAsync(1)).ReturnsAsync(company);
            _mockRepo.Setup(r => r.HasActiveTransportRequestsAsync(1)).ReturnsAsync(true);

            // Act
            // Assert
            await Assert.ThrowsAsync<Exception>(() => _service.DeactivateUserAsync(1));
        }
    }
}
