using Bid_Go_Backend.Controllers;
using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.DTOs;
using Bid_Go_Backend.Data.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Threading.Tasks;
using Xunit;

namespace Bid_Go.Tests.Controllers
{
    public class RegisterDriverControllerTests
    {
        private readonly Mock<IRegisterDriverRepository> _mockRepo;
        private readonly RegisterDriverController _controller;

        public RegisterDriverControllerTests()
        {
            _mockRepo = new Mock<IRegisterDriverRepository>();
            _controller = new RegisterDriverController(_mockRepo.Object);
        }

        [Fact]
        public async Task Register_ShouldReturnBadRequest_WhenModelStateInvalid()
        {
            // Arrange
            _controller.ModelState.AddModelError("Name", "Required");
            var dto = new RegisterDriverDTO();

            // Act
            var result = await _controller.Register(dto);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Register_ShouldReturnConflict_WhenEmailExists()
        {
            // Arrange
            var dto = new RegisterDriverDTO { Email = "exists@test.com" };
            _mockRepo.Setup(r => r.GetByEmailAsync(dto.Email))
                     .ReturnsAsync(new Driver());

            // Act
            var result = await _controller.Register(dto);

            // Assert
            var conflict = Assert.IsType<ConflictObjectResult>(result);
            var val = conflict.Value.GetType().GetProperty("message").GetValue(conflict.Value);
            Assert.Equal("Email is already registered.", val);
        }

        [Fact]
        public async Task Register_ShouldReturnConflict_WhenPhoneExists()
        {
            // Arrange
            var dto = new RegisterDriverDTO { Email = "new@test.com", PhoneNumber = 123456789 };
            _mockRepo.Setup(r => r.GetByEmailAsync(dto.Email)).ReturnsAsync((Driver)null);
            _mockRepo.Setup(r => r.GetByPhoneAsync(dto.PhoneNumber)).ReturnsAsync(new Driver());

            // Act
            var result = await _controller.Register(dto);

            // Assert
            var conflict = Assert.IsType<ConflictObjectResult>(result);
            var val = conflict.Value.GetType().GetProperty("message").GetValue(conflict.Value);
            Assert.Equal("Phone number is already registered.", val);
        }

        [Fact]
        public async Task Register_ShouldReturnConflict_WhenNIFExists()
        {
            // Arrange
            var dto = new RegisterDriverDTO { Email = "new@test.com", PhoneNumber = 123, NIF = 987 };
            _mockRepo.Setup(r => r.GetByEmailAsync(dto.Email)).ReturnsAsync((Driver)null);
            _mockRepo.Setup(r => r.GetByPhoneAsync(dto.PhoneNumber)).ReturnsAsync((Driver)null);
            _mockRepo.Setup(r => r.GetByNIFAsync(dto.NIF)).ReturnsAsync(new Driver());

            // Act
            var result = await _controller.Register(dto);

            // Assert
            var conflict = Assert.IsType<ConflictObjectResult>(result);
            var val = conflict.Value.GetType().GetProperty("message").GetValue(conflict.Value);
            Assert.Equal("Tax ID (NIF) is already registered.", val);
        }

        [Fact]
        public async Task Register_ShouldReturnOk_WhenValid()
        {
            // Arrange
            var dto = new RegisterDriverDTO
            {
                Name = "Test",
                Email = "new@test.com",
                PhoneNumber = 123,
                NIF = 987,
                Password = "pass",
                DriverLicense = "DL123",
                Insurance = "INS123"
            };

            _mockRepo.Setup(r => r.GetByEmailAsync(dto.Email)).ReturnsAsync((Driver)null);
            _mockRepo.Setup(r => r.GetByPhoneAsync(dto.PhoneNumber)).ReturnsAsync((Driver)null);
            _mockRepo.Setup(r => r.GetByNIFAsync(dto.NIF)).ReturnsAsync((Driver)null);
            _mockRepo.Setup(r => r.CreateAsync(It.IsAny<Driver>()))
          .ReturnsAsync((Driver d) =>
          {
              d.Id = 1; // atribui Id simulado
              return d;
          });
            

            // Act
            var result = await _controller.Register(dto);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            var message = ok.Value.GetType().GetProperty("message").GetValue(ok.Value);
            Assert.Equal("Driver account created successfully.", message);
            var driverObj = ok.Value.GetType().GetProperty("driver").GetValue(ok.Value);
            Assert.NotNull(driverObj.GetType().GetProperty("Id").GetValue(driverObj));
            Assert.Equal(dto.Email, driverObj.GetType().GetProperty("Email").GetValue(driverObj));
        }
    }
}
