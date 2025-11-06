using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.DTOs;
using Bid_Go_Backend.Services.Register;
using Moq;
using Xunit;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Text;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using Bid_Go_Backend.Services.Interfaces;
using Bid_Go_Backend.Repositories.Interfaces;

namespace Bid_Go.Tests.Unit.Services
{
    public class RegisterDriverServiceTests
    {
      
        private static IFormFile MakeFormFile(string fileName = "file.png")
        {
            var content = "fake image content";
            var bytes = Encoding.UTF8.GetBytes(content);
            var stream = new MemoryStream(bytes);
            stream.Position = 0;
            return new FormFile(stream, 0, stream.Length, "file", fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = "image/png"
            };
        }

        private static RegisterDriverDTO MakeDto() => new RegisterDriverDTO
        {
            Name = "João Motorista",
            DriverLicense = MakeFormFile("license.png"),
            Insurance = MakeFormFile("insurance.png"),
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

            var mockCloud = new Mock<ICloudflareR2Service>();
            mockCloud.Setup(c => c.UploadImageAsync(It.IsAny<IFormFile>())).ReturnsAsync((IFormFile f) => "https://cdn.example.com/" + f.FileName);

            var service = new RegisterDriverService(repo.Object, mockCloud.Object);
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

            var mockCloud = new Mock<ICloudflareR2Service>();
            var service = new RegisterDriverService(repo.Object, mockCloud.Object);
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

            var mockCloud = new Mock<ICloudflareR2Service>();
            var service = new RegisterDriverService(repo.Object, mockCloud.Object);
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

            var mockCloud = new Mock<ICloudflareR2Service>();
            var service = new RegisterDriverService(repo.Object, mockCloud.Object);
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
