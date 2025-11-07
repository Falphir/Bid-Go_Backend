using Bid_Go_Backend.Controllers;
using Bid_Go_Backend.Data;
using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.DTOs;
using Bid_Go_Backend.Repositories.Register;
using Bid_Go_Backend.Services.Interfaces;
using Bid_Go_Backend.Services.Register;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Caching.Memory;
using System.Text;
using Xunit;

namespace Bid_Go.Tests.Integration
{
    /// <summary>
    /// Integration tests for authentication controller: login, recover and reset password flows.
    /// </summary>
    public class AuthControllerTests
    {
        private static (AuthController controller, BidGoDbContext db, IMemoryCache cache, TestEmailService email) Build()
        {
            var options = new DbContextOptionsBuilder<BidGoDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            var db = new BidGoDbContext(options);
            var userRepo = new Bid_Go_Backend.Repositories.Login.UserRepository(db);
            var email = new TestEmailService();
            var cache = new MemoryCache(new MemoryCacheOptions());
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    {"Jwt:Key", "super_secret_key_123456789_ABCDEF_123456"},
                    {"Jwt:Issuer", "TestIssuer"},
                    {"Jwt:Audience", "TestAudience"},
                    {"Jwt:ExpireMinutes", "60"}
                }).Build();
            var authService = new Bid_Go_Backend.Services.Auth.AuthService(userRepo, email, config, cache);
            var controller = new AuthController(authService);
            return (controller, db, cache, email);
        }

        [Fact]
        public async Task Login_ReturnsOk_WhenCredentialsAreValid_ForDriver()
        {
            // Arrange
            var (controller, db, _, _) = Build();
            var pwd = BCrypt.Net.BCrypt.HashPassword("123456");
            db.Users.Add(new Driver { Email = "driver_login@test.com", Password = pwd, Name = "Test Driver", PhoneNumber =912345678, NIF =123456789 });
            await db.SaveChangesAsync();
            var loginDto = new Bid_Go_Backend.Data.Models.DTOs.LoginDTOs.LoginRequestDto { Email = "driver_login@test.com", Password = "123456" };

            // Act
            var result = await controller.Login(loginDto);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, ok.StatusCode);
        }

        [Fact]
        public async Task Login_ReturnsOk_WhenCredentialsAreValid_ForCompany()
        {
            var (controller, db, _, _) = Build();
            var pwd = BCrypt.Net.BCrypt.HashPassword("123456");
            var company = new Company
            {
                Email = "company_login@test.com",
                Password = pwd,
                Name = "Test Company",
                CompanyName = "Fake Co.",
                PhoneNumber =987654321,
                NIF =999888777
            };
            db.Users.Add(company);
            await db.SaveChangesAsync();

            var loginDto = new Bid_Go_Backend.Data.Models.DTOs.LoginDTOs.LoginRequestDto { Email = "company_login@test.com", Password = "123456" };
            var result = await controller.Login(loginDto);
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, ok.StatusCode);
        }

        [Fact]
        public async Task Login_ReturnsUnauthorized_WhenPasswordIsWrong()
        {
            var (controller, db, _, _) = Build();
            var pwd = BCrypt.Net.BCrypt.HashPassword("123456");
            db.Users.Add(new Driver { Email = "user@test.com", Password = pwd });
            await db.SaveChangesAsync();

            var loginDto = new Bid_Go_Backend.Data.Models.DTOs.LoginDTOs.LoginRequestDto { Email = "user@test.com", Password = "wrong" };
            var result = await controller.Login(loginDto);
            var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal(401, unauthorized.StatusCode);
        }

        [Fact]
        public async Task RecoverPassword_SendsEmail_AndStoresToken()
        {
            var (controller, db, cache, email) = Build();
            db.Users.Add(new Driver { Email = "user@test.com", Password = BCrypt.Net.BCrypt.HashPassword("123456") });
            await db.SaveChangesAsync();

            var request = new RecoverPasswordRequestDTO { Email = "user@test.com" };
            var result = await controller.RecoverPassword(request);
            var ok = Assert.IsType<ObjectResult>(result);
            Assert.Equal(200, ok.StatusCode);
            Assert.Single(email.Sent);
        }

        [Fact]
        public async Task ResetPassword_Works_WhenTokenValid()
        {
            var (controller, db, cache, email) = Build();
            db.Users.Add(new Driver { Email = "user@test.com", Password = BCrypt.Net.BCrypt.HashPassword("123456") });
            await db.SaveChangesAsync();

            var request = new RecoverPasswordRequestDTO { Email = "user@test.com" };
            await controller.RecoverPassword(request);
            var token = email.Sent.First().body.Split('\'')[1];

            var resetDto = new ResetPasswordRequestDTO { Token = token, NewPassword = "nova123" };
            var result = await controller.ResetPassword(resetDto);
            var ok = Assert.IsType<ObjectResult>(result);
            Assert.Equal(200, ok.StatusCode);
        }

        // Email test double inside the file
        private sealed class TestEmailService : IEmailService
        {
            public List<(string to,string subject,string body)> Sent { get; } = new();
            public Task SendEmailAsync(string to, string subject, string body)
            {
                Sent.Add((to, subject, body));
                return Task.CompletedTask;
            }
        }
    }
}
