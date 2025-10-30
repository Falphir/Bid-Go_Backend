using Bid_Go_Backend.Controllers;
using Bid_Go_Backend.Data;
using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.DTOs;
using Bid_Go_Backend.Data.Models.Enums;
using Bid_Go_Backend.Data.Repositories.Interfaces;
using Bid_Go_Backend.Repositories.Interface;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using Xunit;
using Bid_Go_Backend.Services;

namespace Bid_Go.Tests.Controllers
{
    public class AuthControllerTests
    {
        private readonly Mock<IUserRepository> _mockRepo;
        private readonly Mock<AuthService> _mockAuthService;
        private readonly Mock<EmailService> _mockEmailService;
        private readonly IMemoryCache _memoryCache;
        private readonly BidGoDbContext _context;
        private readonly AuthController _authController;

        public AuthControllerTests()
        {
            _mockRepo = new Mock<IUserRepository>();

            var options = new DbContextOptionsBuilder<BidGoDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new BidGoDbContext(options);
            _context.Database.EnsureCreated();

            // real cache
            _memoryCache = new MemoryCache(new MemoryCacheOptions());

            // create a mock of AuthService passing the required IMemoryCache to the constructor
            _mockAuthService = new Mock<AuthService>(_memoryCache);

            // create a mock of EmailService providing constructor args
            _mockEmailService = new Mock<EmailService>("smtp.example.com",25, "from@example.com", "pass");

            _authController = new AuthController(
                _mockRepo.Object,
                _mockAuthService.Object,
                _mockEmailService.Object,
                _memoryCache
            );
        }

        [Fact]
        public async Task RecoverPassword_ShouldReturnNotFound_WhenUserDoesNotExist()
        {
            // Arrange
            var dto = new RecoverPasswordRequestDTO { Email = "test@test.com" };
            _mockRepo.Setup(r => r.GetByEmailAsync(dto.Email)).ReturnsAsync((User)null);

            // Act
            var result = await _authController.RecoverPassword(dto);

            // Assert
            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            var val = notFound.Value;
            var prop = val.GetType().GetProperty("message");
            Assert.NotNull(prop);
            Assert.Equal("Utilizador não encontrado.", prop.GetValue(val));
        }

        [Fact]
        public async Task RecoverPassword_ShouldReturnOkAndSetCache_WhenUserExists()
        {
            // Arrange
            var user = new Driver { Id =1, Email = "test@test.com", Name = "test", Password = "test", PhoneNumber =123456789, NIF =123456789 };
            _mockRepo.Setup(r => r.GetByEmailAsync(user.Email)).ReturnsAsync(user);

            _mockAuthService.Setup(s => s.GeneratePasswordResetToken()).Returns("token123");
            _mockEmailService.Setup(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            var dto = new RecoverPasswordRequestDTO { Email = user.Email };

            // Act
            var result = await _authController.RecoverPassword(dto);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            var val = ok.Value;
            var prop = val.GetType().GetProperty("message");
            Assert.NotNull(prop);
            Assert.Equal("Instruções enviadas por email.", prop.GetValue(val));

            // Verify cache contains token -> email
            Assert.True(_memoryCache.TryGetValue("token123", out var cachedEmail));
            Assert.Equal(user.Email, cachedEmail);

            _mockEmailService.Verify(e => e.SendEmailAsync(user.Email, It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task ResetPassword_ShouldReturnBadRequest_WhenTokenInvalid()
        {
            // Arrange
            var req = new ResetPasswordRequestDTO { Token = "invalid", NewPassword = "newpass" };

            // Act
            var result = await _authController.ResetPassword(req);

            // Assert
            var bad = Assert.IsType<BadRequestObjectResult>(result);
            var val = bad.Value;
            var prop = val.GetType().GetProperty("message");
            Assert.NotNull(prop);
            Assert.Equal("Token inválido ou expirado.", prop.GetValue(val));
        }

        [Fact]
        public async Task ResetPassword_ShouldReturnNotFound_WhenUserNotFound()
        {
            // Arrange
            var token = "tok1";
            _memoryCache.Set(token, "test@test.com", TimeSpan.FromHours(1));

            _mockRepo.Setup(r => r.GetByEmailAsync("test@test.com")).ReturnsAsync((User)null);

            var req = new ResetPasswordRequestDTO { Token = token, NewPassword = "newpass" };

            // Act
            var result = await _authController.ResetPassword(req);

            // Assert
            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            var val = notFound.Value;
            var prop = val.GetType().GetProperty("message");
            Assert.NotNull(prop);
            Assert.Equal("Utilizador não encontrado.", prop.GetValue(val));
        }

        [Fact]
        public async Task ResetPassword_ShouldReturnOkAndRemoveToken_WhenSuccessful()
        {
            // Arrange
            var user = new Driver { Id =2, Email = "test@test.com", Name = "test", Password = "test", PhoneNumber =123456789, NIF =123456789 };
            var token = "tok2";
            _memoryCache.Set(token, user.Email, TimeSpan.FromHours(1));

            _mockRepo.Setup(r => r.GetByEmailAsync(user.Email)).ReturnsAsync(user);
            _mockRepo.Setup(r => r.UpdateAsync(It.IsAny<User>())).ReturnsAsync(user);

            var req = new ResetPasswordRequestDTO { Token = token, NewPassword = "newpass" };

            // Act
            var result = await _authController.ResetPassword(req);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            var val = ok.Value;
            var prop = val.GetType().GetProperty("message");
            Assert.NotNull(prop);
            Assert.Equal("Password alterada com sucesso.", prop.GetValue(val));

            // Verify token removed from cache
            Assert.False(_memoryCache.TryGetValue(token, out _));
        }
    }
}
