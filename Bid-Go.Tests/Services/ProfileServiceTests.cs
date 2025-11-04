
using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.DTOs;
using Bid_Go_Backend.Repositories.Interface;
using Bid_Go_Backend.Services.Profile;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Bid_Go.Tests.Services
{
    public class ProfileServiceTests
    {
        private readonly Mock<IProfileRepository> _mockRepo;
        private readonly ProfileService _service;

        public ProfileServiceTests()
        {
            _mockRepo = new Mock<IProfileRepository>();
            _service = new ProfileService(_mockRepo.Object);
        }

        // === GetProfileAsync ===
        [Fact]
        public async Task GetProfileAsync_ReturnsUser_WhenActive()
        {
            var user = new Driver { Id = 1, IsActive = true };
            _mockRepo.Setup(r => r.GetUserByIdAsync(1)).ReturnsAsync(user);

            var result = await _service.GetProfileAsync(1);

            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
        }

        [Fact]
        public async Task GetProfileAsync_ReturnsNull_WhenNotFound()
        {
            _mockRepo.Setup(r => r.GetUserByIdAsync(1)).ReturnsAsync((User?)null);

            var result = await _service.GetProfileAsync(1);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetProfileAsync_ThrowsException_WhenInactive()
        {
            var user = new Driver { Id = 1, IsActive = false };
            _mockRepo.Setup(r => r.GetUserByIdAsync(1)).ReturnsAsync(user);

            await Assert.ThrowsAsync<Exception>(() => _service.GetProfileAsync(1));
        }

        // === UpdateProfileAsync (Driver) ===
        [Fact]
        public async Task UpdateProfileAsync_UpdatesDriverSuccessfully()
        {
            var driver = new Driver
            {
                Id = 1,
                IsActive = true,
                Name = "Old",
                Email = "old@email.com"
            };

            var dto = new DriverProfileDTO
            {
                Name = "New",
                Email = "new@email.com"
            };

            _mockRepo.Setup(r => r.GetUserByIdAsync(1)).ReturnsAsync(driver);
            _mockRepo
        .Setup(r => r.UpdateDriverAsync(It.IsAny<Driver>()))
        .ReturnsAsync(true);

            var result = await _service.UpdateProfileAsync(1, dto);

            Assert.True(result);
            _mockRepo.Verify(r => r.UpdateDriverAsync(It.IsAny<Driver>()), Times.Once);
        }

        [Fact]
        public async Task UpdateProfileAsync_Throws_WhenDriverHasNoChanges()
        {
            var driver = new Driver { Id = 1, IsActive = true, Name = "Same" };
            var dto = new DriverProfileDTO { Name = "Same" };

            _mockRepo.Setup(r => r.GetUserByIdAsync(1)).ReturnsAsync(driver);

            await Assert.ThrowsAsync<Exception>(() => _service.UpdateProfileAsync(1, dto));
        }

        [Fact]
        public async Task UpdateProfileAsync_Throws_WhenInvalidDriverDto()
        {
            var driver = new Driver { Id = 1, IsActive = true };
            _mockRepo.Setup(r => r.GetUserByIdAsync(1)).ReturnsAsync(driver);

            await Assert.ThrowsAsync<Exception>(() => _service.UpdateProfileAsync(1, new CompanyProfileDTO()));
        }

        // === UpdateProfileAsync (Company) ===
        [Fact]
        public async Task UpdateProfileAsync_UpdatesCompanySuccessfully()
        {
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


            var result = await _service.UpdateProfileAsync(1, dto);

            Assert.True(result);
            _mockRepo.Verify(r => r.UpdateCompanyAsync(It.IsAny<Company>()), Times.Once);
        }

        [Fact]
        public async Task UpdateProfileAsync_Throws_WhenInvalidCompanyDto()
        {
            var company = new Company { Id = 1, IsActive = true };
            _mockRepo.Setup(r => r.GetUserByIdAsync(1)).ReturnsAsync(company);

            await Assert.ThrowsAsync<Exception>(() => _service.UpdateProfileAsync(1, new DriverProfileDTO()));
        }

        [Fact]
        public async Task UpdateProfileAsync_Throws_WhenUserInactive()
        {
            var user = new Company { Id = 1, IsActive = false };
            _mockRepo.Setup(r => r.GetUserByIdAsync(1)).ReturnsAsync(user);

            await Assert.ThrowsAsync<Exception>(() => _service.UpdateProfileAsync(1, new DriverProfileDTO()));
        }

        // === ChangePasswordAsync ===
        [Fact]
        public async Task ChangePasswordAsync_UpdatesPassword_WhenValid()
        {
            var hashed = BCrypt.Net.BCrypt.HashPassword("oldpass");
            var user = new Driver { Id = 1, IsActive = true, Password = hashed };

            _mockRepo.Setup(r => r.GetUserByIdAsync(1)).ReturnsAsync(user);
            _mockRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            var result = await _service.ChangePasswordAsync(1, "oldpass", "newpass");

            Assert.True(result);
            _mockRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task ChangePasswordAsync_Throws_WhenCurrentPasswordIncorrect()
        {
            var hashed = BCrypt.Net.BCrypt.HashPassword("correct");
            var user = new Company { Id = 1, IsActive = true, Password = hashed };

            _mockRepo.Setup(r => r.GetUserByIdAsync(1)).ReturnsAsync(user);

            await Assert.ThrowsAsync<Exception>(() => _service.ChangePasswordAsync(1, "wrong", "new"));
        }

        // === DeactivateUserAsync ===
        [Fact]
        public async Task DeactivateUserAsync_Throws_WhenUserNotFound()
        {
            _mockRepo.Setup(r => r.GetUserByIdAsync(1)).ReturnsAsync((User?)null);

            await Assert.ThrowsAsync<Exception>(() => _service.DeactivateUserAsync(1));
        }

        [Fact]
        public async Task DeactivateUserAsync_Throws_WhenAlreadyInactive()
        {
            var user = new Company { Id = 1, IsActive = false };
            _mockRepo.Setup(r => r.GetUserByIdAsync(1)).ReturnsAsync(user);

            await Assert.ThrowsAsync<Exception>(() => _service.DeactivateUserAsync(1));
        }

        [Fact]
        public async Task DeactivateUserAsync_DeactivatesDriver_WhenNoActiveBids()
        {
            var driver = new Driver { Id = 1, IsActive = true };

            _mockRepo.Setup(r => r.GetUserByIdAsync(1)).ReturnsAsync(driver);
            _mockRepo.Setup(r => r.HasActiveBidsAsync(1)).ReturnsAsync(false);
            _mockRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            var result = await _service.DeactivateUserAsync(1);

            Assert.True(result);
            Assert.False(driver.IsActive);
        }

        [Fact]
        public async Task DeactivateUserAsync_Throws_WhenDriverHasActiveBids()
        {
            var driver = new Driver { Id = 1, IsActive = true };

            _mockRepo.Setup(r => r.GetUserByIdAsync(1)).ReturnsAsync(driver);
            _mockRepo.Setup(r => r.HasActiveBidsAsync(1)).ReturnsAsync(true);

            await Assert.ThrowsAsync<Exception>(() => _service.DeactivateUserAsync(1));
        }

        [Fact]
        public async Task DeactivateUserAsync_Throws_WhenCompanyHasActiveRequests()
        {
            var company = new Company { Id = 1, IsActive = true };

            _mockRepo.Setup(r => r.GetUserByIdAsync(1)).ReturnsAsync(company);
            _mockRepo.Setup(r => r.HasActiveTransportRequestsAsync(1)).ReturnsAsync(true);

            await Assert.ThrowsAsync<Exception>(() => _service.DeactivateUserAsync(1));
        }
    }
}
