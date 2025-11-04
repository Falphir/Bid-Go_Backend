using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.DTOs;
using Bid_Go_Backend.Data.Repositories.Interfaces;
using Bid_Go_Backend.Services.Register;
using Moq;
using Xunit;

namespace Bid_Go.Tests.Services
{
    public class RegisterDriverServiceTests
    {
        private static RegisterDriverDTO MakeDto() => new RegisterDriverDTO
        {
            Name = "João Motorista",
            DriverLicense = "DL-PT-12345",
            Insurance = "INS-0001",
            Email = "driver@example.com",
            Password = "PlainPassword123!",
            PhoneNumber = 912345678,
            NIF = 123456789
        };

        [Fact]
        public async Task RegisterAsync_ShouldReturnSuccess_AndHashPassword_WhenAllUnique()
        {
            // Arrange
            var repo = new Mock<IRegisterDriverRepository>();

            // Unicidade
            repo.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((Driver?)null);
            repo.Setup(r => r.GetByPhoneAsync(It.IsAny<int>())).ReturnsAsync((Driver?)null);
            repo.Setup(r => r.GetByNIFAsync(It.IsAny<int>())).ReturnsAsync((Driver?)null);

            Driver? captured = null;
            repo.Setup(r => r.CreateAsync(It.IsAny<Driver>()))
                .Callback<Driver>(d => { d.Id = 77; captured = d; })
                .ReturnsAsync((Driver d) => d);

            var service = new RegisterDriverService(repo.Object);
            var dto = MakeDto();

            // Act
            var (success, error, driver) = await service.RegisterAsync(dto);

            // Assert
            Assert.True(success);
            Assert.Null(error);
            Assert.NotNull(driver);
            Assert.Equal(77, driver!.Id);
            Assert.Equal(dto.Email, driver.Email);
            Assert.Equal(dto.PhoneNumber, driver.PhoneNumber);
            Assert.Equal(dto.NIF, driver.NIF);

            Assert.NotNull(captured);
            Assert.NotEqual(dto.Password, captured!.Password);
            Assert.False(string.IsNullOrWhiteSpace(captured.Password));

            repo.Verify(r => r.CreateAsync(It.IsAny<Driver>()), Times.Once);
        }

        [Fact]
        public async Task RegisterAsync_ShouldFail_WhenEmailExists()
        {
            // Arrange
            var repo = new Mock<IRegisterDriverRepository>();
            repo.Setup(r => r.GetByEmailAsync(It.IsAny<string>()))
                .ReturnsAsync(new Driver { Email = "driver@example.com" });

            var service = new RegisterDriverService(repo.Object);
            var dto = MakeDto();

            // Act
            var (success, error, driver) = await service.RegisterAsync(dto);

            // Assert
            Assert.False(success);
            Assert.Equal("EMAIL_EXISTS", error);
            Assert.Null(driver);
            repo.Verify(r => r.CreateAsync(It.IsAny<Driver>()), Times.Never);
        }

        [Fact]
        public async Task RegisterAsync_ShouldFail_WhenPhoneExists()
        {
            // Arrange
            var repo = new Mock<IRegisterDriverRepository>();
            repo.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((Driver?)null);
            repo.Setup(r => r.GetByPhoneAsync(It.IsAny<int>()))
                .ReturnsAsync(new Driver { PhoneNumber = 912345678 });

            var service = new RegisterDriverService(repo.Object);
            var dto = MakeDto();

            // Act
            var (success, error, driver) = await service.RegisterAsync(dto);

            // Assert
            Assert.False(success);
            Assert.Equal("PHONE_EXISTS", error);
            Assert.Null(driver);
            repo.Verify(r => r.CreateAsync(It.IsAny<Driver>()), Times.Never);
        }

        [Fact]
        public async Task RegisterAsync_ShouldFail_WhenNifExists()
        {
            // Arrange
            var repo = new Mock<IRegisterDriverRepository>();
            repo.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((Driver?)null);
            repo.Setup(r => r.GetByPhoneAsync(It.IsAny<int>())).ReturnsAsync((Driver?)null);
            repo.Setup(r => r.GetByNIFAsync(It.IsAny<int>()))
                .ReturnsAsync(new Driver { NIF = 123456789 });

            var service = new RegisterDriverService(repo.Object);
            var dto = MakeDto();

            // Act
            var (success, error, driver) = await service.RegisterAsync(dto);

            // Assert
            Assert.False(success);
            Assert.Equal("NIF_EXISTS", error);
            Assert.Null(driver);
            repo.Verify(r => r.CreateAsync(It.IsAny<Driver>()), Times.Never);
        }
    }
}
