using Bid_Go.Tests.Integration.Utils;
using Bid_Go_Backend.Controllers;
using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.DTOs;
using Bid_Go_Backend.Data.Models.DTOs.LoginDTOs;
using Bid_Go_Backend.Repositories.Interfaces;
using Bid_Go_Backend.Services.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Bid_Go.Tests.Integration.Controllers
{
    public class AuthControllerTests
    {
        private readonly AuthController _controller;
        private readonly FakeUserRepository _userRepo;
        private readonly FakeEmailService _emailService;
        private readonly FakeMemoryCache _cache;

        //Fakes para simular drivers e companies
        private readonly FakeRegisterDriverRepository _driverRepo;
        private readonly FakeRegisterCompanyRepository _companyRepo;

        public AuthControllerTests()
        {
            _userRepo = new FakeUserRepository();
            _emailService = new FakeEmailService();
            _cache = new FakeMemoryCache();
            _driverRepo = new FakeRegisterDriverRepository();
            _companyRepo = new FakeRegisterCompanyRepository();

            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    {"Jwt:Key", "super_secret_key_123456789_ABCDEF_123456"},
                    {"Jwt:Issuer", "TestIssuer"},
                    {"Jwt:Audience", "TestAudience"},
                    {"Jwt:ExpireMinutes", "60"}
                })
                .Build();

            var authService = new AuthService(_userRepo, _emailService, config, _cache);
            _controller = new AuthController(authService);
        }

        [Fact]
        public async Task Login_ReturnsOk_WhenCredentialsAreValid_ForDriver()
        {
            // Arrange
            var driver = await _driverRepo.CreateAsync(new Driver
            {
                Email = "driver_login@test.com",
                Password = BCrypt.Net.BCrypt.HashPassword("123456"),
                Name = "Test Driver",
                PhoneNumber = 912345678,
                NIF = 123456789
            });

            await _userRepo.CreateUserAsync(new Driver
            {
                Id = driver.Id,
                Email = driver.Email,
                Password = driver.Password
            });

            var loginDto = new LoginRequestDto { Email = "driver_login@test.com", Password = "123456" };

            // Act
            var result = await _controller.Login(loginDto);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, ok.StatusCode);
        }

        [Fact]
        public async Task Login_ReturnsOk_WhenCredentialsAreValid_ForCompany()
        {
            // Arrange
            var company = await _companyRepo.CreateAsync(new Company
            {
                Email = "company_login@test.com",
                Password = BCrypt.Net.BCrypt.HashPassword("123456"),
                Name = "Test Company",
                CompanyName = "Fake Co.",
                PhoneNumber = 987654321,
                NIF = 999888777
            });

            await _userRepo.CreateUserAsync(new Company
            {
                Id = company.Id,
                Email = company.Email,
                Password = company.Password
            });

            var loginDto = new LoginRequestDto { Email = "company_login@test.com", Password = "123456" };

            // Act
            var result = await _controller.Login(loginDto);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, ok.StatusCode);
        }

        [Fact]
        public async Task Login_ReturnsUnauthorized_WhenPasswordIsWrong()
        {
            // Arrange
            var loginDto = new LoginRequestDto { Email = "user@test.com", Password = "wrong" };

            // Act
            var result = await _controller.Login(loginDto);

            // Assert
            var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal(401, unauthorized.StatusCode);
        }

        [Fact]
        public async Task RecoverPassword_SendsEmail_AndStoresToken()
        {
            // Arrange
            var request = new RecoverPasswordRequestDTO { Email = "user@test.com" };

            // Act
            var result = await _controller.RecoverPassword(request);

            // Assert
            var ok = Assert.IsType<ObjectResult>(result);
            Assert.Equal(200, ok.StatusCode);
            Assert.Single(_emailService.SentEmails);
        }

        [Fact]
        public async Task ResetPassword_Works_WhenTokenValid()
        {
            // Arrange
            var request = new RecoverPasswordRequestDTO { Email = "user@test.com" };
            await _controller.RecoverPassword(request);
            var token = _emailService.SentEmails.First().body.Split('\'')[1]; // extrai token

            // Act
            var resetDto = new ResetPasswordRequestDTO { Token = token, NewPassword = "nova123" };
            var result = await _controller.ResetPassword(resetDto);

            // Assert
            var ok = Assert.IsType<ObjectResult>(result);
            Assert.Equal(200, ok.StatusCode);
        }
    }
}
