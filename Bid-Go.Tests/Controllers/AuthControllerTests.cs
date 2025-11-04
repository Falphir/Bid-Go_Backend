using Bid_Go_Backend.Controllers;
using Bid_Go_Backend.Data;
using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.DTOs;
using Bid_Go_Backend.Data.Models.DTOs.LoginDTOs;
using Bid_Go_Backend.Data.Repositories.Interfaces;
using Bid_Go_Backend.Repositories.Interface;
using Bid_Go_Backend.Services;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Moq;
using System.Security.Claims;
using Xunit;

namespace Bid_Go.Tests.Controllers
{
    public class AuthControllerTests
    {
        private readonly Mock<IUserRepository> _mockRepo;
        private readonly Mock<IAuthRepository> _mockAuthService;
        private readonly Mock<IEmailService> _mockEmailService;
        private readonly IMemoryCache _memoryCache;
        private readonly BidGoDbContext _context;
        private readonly AuthController _authController;

        public AuthControllerTests()
        {
            // Repositório mockado
            _mockRepo = new Mock<IUserRepository>();

            // BD em memória
            var options = new DbContextOptionsBuilder<BidGoDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new BidGoDbContext(options);
            _context.Database.EnsureCreated();

            // Cache real
            _memoryCache = new MemoryCache(new MemoryCacheOptions());

            // Services mockados via interface (melhor prática)
            _mockAuthService = new Mock<IAuthRepository>();
            _mockEmailService = new Mock<IEmailService>();

            // Controller que será testado
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
            var dto = new RecoverPasswordRequestDTO { Email = "test@test.com" };
            _mockRepo.Setup(r => r.GetByEmailAsync(dto.Email)).ReturnsAsync((User)null);

            var result = await _authController.RecoverPassword(dto);

            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            var val = notFound.Value;
            var prop = val.GetType().GetProperty("message");
            Assert.NotNull(prop);
            Assert.Equal("Utilizador não encontrado.", prop.GetValue(val));
        }

        [Fact]
        public async Task RecoverPassword_ShouldReturnOkAndSetCache_WhenUserExists()
        {
            var user = new Driver
            {
                Id = 1,
                Email = "test@test.com",
                Name = "test",
                Password = "test",
                PhoneNumber = 123456789,
                NIF = 123456789
            };

            _mockRepo.Setup(r => r.GetByEmailAsync(user.Email)).ReturnsAsync(user);
            _mockAuthService.Setup(s => s.GeneratePasswordResetToken()).Returns("token123");
            _mockEmailService.Setup(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            var dto = new RecoverPasswordRequestDTO { Email = user.Email };

            var result = await _authController.RecoverPassword(dto);

            var ok = Assert.IsType<OkObjectResult>(result);
            var val = ok.Value;
            var prop = val.GetType().GetProperty("message");
            Assert.NotNull(prop);
            Assert.Equal("Instruções enviadas por email.", prop.GetValue(val));

            Assert.True(_memoryCache.TryGetValue("token123", out var cachedEmail));
            Assert.Equal(user.Email, cachedEmail);

            _mockEmailService.Verify(e => e.SendEmailAsync(user.Email, It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task ResetPassword_ShouldReturnBadRequest_WhenTokenInvalid()
        {
            var req = new ResetPasswordRequestDTO { Token = "invalid", NewPassword = "newpass" };

            var result = await _authController.ResetPassword(req);

            var bad = Assert.IsType<BadRequestObjectResult>(result);
            var val = bad.Value;
            var prop = val.GetType().GetProperty("message");
            Assert.NotNull(prop);
            Assert.Equal("Token inválido ou expirado.", prop.GetValue(val));
        }

        [Fact]
        public async Task ResetPassword_ShouldReturnNotFound_WhenUserNotFound()
        {
            var token = "tok1";
            _memoryCache.Set(token, "test@test.com", TimeSpan.FromHours(1));
            _mockRepo.Setup(r => r.GetByEmailAsync("test@test.com")).ReturnsAsync((User)null);

            var req = new ResetPasswordRequestDTO { Token = token, NewPassword = "newpass" };

            var result = await _authController.ResetPassword(req);

            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            var val = notFound.Value;
            var prop = val.GetType().GetProperty("message");
            Assert.NotNull(prop);
            Assert.Equal("Utilizador não encontrado.", prop.GetValue(val));
        }

        [Fact]
        public async Task ResetPassword_ShouldReturnOkAndRemoveToken_WhenSuccessful()
        {
            var user = new Driver { Id = 2, Email = "test@test.com", Password = "test" };
            var token = "tok2";
            _memoryCache.Set(token, user.Email, TimeSpan.FromHours(1));

            _mockRepo.Setup(r => r.GetByEmailAsync(user.Email)).ReturnsAsync(user);
            _mockRepo.Setup(r => r.UpdateAsync(It.IsAny<User>())).ReturnsAsync(user);

            var req = new ResetPasswordRequestDTO { Token = token, NewPassword = "newpass" };

            var result = await _authController.ResetPassword(req);

            var ok = Assert.IsType<OkObjectResult>(result);
            var val = ok.Value;
            var prop = val.GetType().GetProperty("message");
            Assert.NotNull(prop);
            Assert.Equal("Password alterada com sucesso.", prop.GetValue(val));

            Assert.False(_memoryCache.TryGetValue(token, out _));
        }

        [Fact]
        public async Task Login_ShouldReturnUnauthorized_WhenEmailInvalid()
        {
            var req = new LoginRequestDto { Email = "nope@test.com", Password = "whatever" };
            _mockRepo.Setup(r => r.GetByEmailAsync(req.Email)).ReturnsAsync((User)null);

            var result = await _authController.Login(req);

            var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
            var val = unauthorized.Value;
            var prop = val.GetType().GetProperty("message");
            Assert.NotNull(prop);
            Assert.Equal("Email inválido.", prop.GetValue(val));
        }

        [Fact]
        public async Task Login_ShouldReturnUnauthorized_WhenPasswordIncorrect()
        {
            var hashed = BCrypt.Net.BCrypt.HashPassword("correctPassword");
            var user = new Driver { Id = 3, Email = "user@test.com", Password = hashed };
            _mockRepo.Setup(r => r.GetByEmailAsync(user.Email)).ReturnsAsync(user);

            var req = new LoginRequestDto { Email = user.Email, Password = "wrong" };

            var result = await _authController.Login(req);

            var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
            var val = unauthorized.Value;
            var prop = val.GetType().GetProperty("message");
            Assert.NotNull(prop);
            Assert.Equal("Password incorreta.", prop.GetValue(val));
        }

        [Fact]
        public async Task Login_ShouldReturnOkWithToken_WhenCredentialsValid()
        {
            var plain = "mypassword";
            var hashed = BCrypt.Net.BCrypt.HashPassword(plain);
            var user = new Driver { Id = 4, Email = "valid@test.com", Password = hashed };

            _mockRepo.Setup(r => r.GetByEmailAsync(user.Email)).ReturnsAsync(user);
            _mockAuthService.Setup(s => s.GenerateJwtToken(It.IsAny<User>())).Returns("jwtToken123");

            var req = new LoginRequestDto { Email = user.Email, Password = plain };

            var result = await _authController.Login(req);

            var ok = Assert.IsType<OkObjectResult>(result);
            var tokenProp = ok.Value.GetType().GetProperty("Token");
            Assert.NotNull(tokenProp);
            Assert.Equal("jwtToken123", tokenProp.GetValue(ok.Value));
        }

        [Fact]
        public void Me_ShouldReturnOk_WithEmailFromClaims()
        {
            var email = "claimed@test.com";
            var claims = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim("sub", email),
                new Claim("userId", "10"),
                new Claim("userType", "Driver")
            }, "mock"));

            _authController.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claims }
            };

            var result = _authController.Me();

            var ok = Assert.IsType<OkObjectResult>(result);
            var val = ok.Value;
            var prop = val.GetType().GetProperty("email");
            Assert.NotNull(prop);
            Assert.Equal(email, prop.GetValue(val));
        }

        [Fact]
        public void CompanyEndpoint_ShouldReturnOk_WhenCalled()
        {
            var result = _authController.CompanyEndpoint();
            var ok = Assert.IsType<OkObjectResult>(result);
            var val = ok.Value;
            var prop = val.GetType().GetProperty("message");
            Assert.NotNull(prop);
            Assert.Equal("Apenas Companies conseguem ver isto!", prop.GetValue(val));
        }

        [Fact]
        public void DriverEndpoint_ShouldReturnOk_WhenCalled()
        {
            var result = _authController.DriverEndpoint();
            var ok = Assert.IsType<OkObjectResult>(result);
            var val = ok.Value;
            var prop = val.GetType().GetProperty("message");
            Assert.NotNull(prop);
            Assert.Equal("Apenas Drivers conseguem ver isto!", prop.GetValue(val));
        }
    }
}
