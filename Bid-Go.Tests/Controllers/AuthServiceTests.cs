using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using Xunit;
using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Repositories.Interfaces;
using Bid_Go_Backend.Services.Auth;
using Bid_Go_Backend.Services.Interfaces;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _userRepoMock;
    private readonly Mock<IEmailService> _emailMock;
    private readonly Mock<IConfiguration> _configMock;
    private readonly IMemoryCache _cache;
    private readonly AuthService _service;

    public AuthServiceTests()
    {
        _userRepoMock = new Mock<IUserRepository>();
        _emailMock = new Mock<IEmailService>();
        _configMock = new Mock<IConfiguration>();

 
        _configMock.Setup(c => c["Jwt:Key"]).Returns("supersecretkey1234567890");
        _configMock.Setup(c => c["Jwt:Issuer"]).Returns("TestIssuer");
        _configMock.Setup(c => c["Jwt:Audience"]).Returns("TestAudience");
        _configMock.Setup(c => c["Jwt:ExpireMinutes"]).Returns("60");

        _cache = new MemoryCache(new MemoryCacheOptions());

        _service = new AuthService(_userRepoMock.Object, _emailMock.Object, _configMock.Object, _cache);
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnFalse_WhenUserNotFound()
    {
        _userRepoMock.Setup(r => r.GetByEmailAsync("teste@test.com")).ReturnsAsync((User)null);

        var result = await _service.LoginAsync("teste@test.com", "123");

        Assert.False(result.Success);
        Assert.Equal("Email inválido.", result.Message);
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnFalse_WhenPasswordIncorrect()
    {
        
        var user = new Driver
        {
            Id = 1,
            Name = "Teste",
            Email = "teste@test.com",
            Password = BCrypt.Net.BCrypt.HashPassword("correct"),
            PhoneNumber = 912345678,
            NIF = 123456789,
            IsActive = true
        };
        _userRepoMock.Setup(r => r.GetByEmailAsync("teste@test.com")).ReturnsAsync(user);

        var result = await _service.LoginAsync("teste@test.com", "wrong");

        Assert.False(result.Success);
        Assert.Equal("Password incorreta.", result.Message);
    }


    [Fact]
    public async Task RecoverPasswordAsync_ShouldSendEmailAndCacheToken_WhenUserExists()
    {
        var user = new Driver
        {
            Id = 1,
            Name = "Teste",
            Email = "teste@test.com",
            Password = BCrypt.Net.BCrypt.HashPassword("correct"),
            PhoneNumber = 912345678,
            NIF = 123456789,
            IsActive = true
        };
        _userRepoMock.Setup(r => r.GetByEmailAsync("teste@test.com")).ReturnsAsync(user);
        _emailMock.Setup(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                  .Returns(Task.CompletedTask);

        var result = await _service.RecoverPasswordAsync("teste@test.com");

        Assert.Equal(200, result.StatusCode);
        Assert.Equal("Instruções enviadas por email.", result.Message);
    }

    [Fact]
    public async Task ResetPasswordAsync_ShouldUpdatePassword_WhenTokenValid()
    {
        var user = new Driver
        {
            Id = 1,
            Name = "Teste",
            Email = "teste@test.com",
            Password = BCrypt.Net.BCrypt.HashPassword("old"),
            PhoneNumber = 912345678,
            NIF = 123456789,
            IsActive = true
        };
        var token = Guid.NewGuid().ToString("N");
        _cache.Set(token, user.Email, TimeSpan.FromHours(1));
        _userRepoMock.Setup(r => r.GetByEmailAsync(user.Email)).ReturnsAsync(user);
        _userRepoMock.Setup(r => r.UpdateAsync(It.IsAny<User>())).ReturnsAsync(user);

        var result = await _service.ResetPasswordAsync(token, "newpassword");

        Assert.Equal(200, result.StatusCode);
        Assert.Equal("Password alterada com sucesso.", result.Message);
        Assert.True(BCrypt.Net.BCrypt.Verify("newpassword", user.Password));
    }
}
