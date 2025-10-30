using System.Text.Json;
using Bid_Go_Backend.Controllers;
using Bid_Go_Backend.Data;
using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.DTOs;
using Bid_Go_Backend.Repositories.Interface;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Bid_Go_Backend.Tests.Controllers
{
    public class ProfileControllerTests
    {
        private readonly Mock<IProfileCrud> _mockRepo;
        private readonly BidGoDbContext _context;
        private readonly ProfileController _controller;

        public ProfileControllerTests()
        {
            _mockRepo = new Mock<IProfileCrud>();

            var options = new DbContextOptionsBuilder<BidGoDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new BidGoDbContext(options);
            _context.Database.EnsureCreated();

            _controller = new ProfileController(_mockRepo.Object, _context);
        }

        // GET /api/profile/{id}
        [Fact]
        public async Task GetProfile_ShouldReturnNotFound_WhenUserDoesNotExist()
        {
            _mockRepo.Setup(r => r.GetUserByIdAsync(1)).ReturnsAsync((User?)null);

            var result = await _controller.GetProfile(1);

            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("User not found", notFound.Value);
        }

        [Fact]
        public async Task GetProfile_ShouldReturnConflict_WhenUserIsInactive()
        {
            var user = new Driver { IsActive = false };
            _mockRepo.Setup(r => r.GetUserByIdAsync(2)).ReturnsAsync(user);

            var result = await _controller.GetProfile(2);

            var conflict = Assert.IsType<ConflictObjectResult>(result);
            Assert.Equal("User is inactive and cannot be updated.", conflict.Value);
        }

        [Fact]
        public async Task GetProfile_ShouldReturnDriverDto_WhenUserIsDriver()
        {
            var driver = new Driver
            {
                Name = "D",
                Email = "d@ex.com",
                PhoneNumber = 999,
                NIF = 123456789,
                DriverLicense = "L123",
                Insurance = "Ins",
                IsActive = true
            };

            _mockRepo.Setup(r => r.GetUserByIdAsync(3)).ReturnsAsync(driver);

            var result = await _controller.GetProfile(3);

            var ok = Assert.IsType<OkObjectResult>(result);
            var dto = Assert.IsType<DriverProfileDTO>(ok.Value);
            Assert.Equal(driver.Name, dto.Name);
            Assert.Equal(driver.Email, dto.Email);
            Assert.Equal(driver.DriverLicense, dto.DriverLicense);
            Assert.Equal(driver.Insurance, dto.Insurance);
            Assert.Equal(driver.PhoneNumber, dto.PhoneNumber);
            Assert.Equal(driver.NIF, dto.NIF);
        }

        [Fact]
        public async Task GetProfile_ShouldReturnCompanyDto_WhenUserIsCompany()
        {
            var company = new Company
            {
                Name = "C",
                Email = "c@ex.com",
                PhoneNumber = 888,
                NIF = 999999999,
                CompanyName = "My Co",
                Address = "Street 1",
                IsActive = true
            };

            _mockRepo.Setup(r => r.GetUserByIdAsync(4)).ReturnsAsync(company);

            var result = await _controller.GetProfile(4);

            var ok = Assert.IsType<OkObjectResult>(result);
            var dto = Assert.IsType<CompanyProfileDTO>(ok.Value);
            Assert.Equal(company.CompanyName, dto.CompanyName);
            Assert.Equal(company.Address, dto.Address);
            Assert.Equal(company.Email, dto.Email);
            Assert.Equal(company.PhoneNumber, dto.PhoneNumber);
            Assert.Equal(company.NIF, dto.NIF);
        }



        // PUT /api/profile/{id}
        [Fact]
        public async Task UpdateProfile_ShouldReturnNotFound_WhenUserDoesNotExist()
        {
            _mockRepo.Setup(r => r.GetUserByIdAsync(10)).ReturnsAsync((User?)null);

            using var doc = JsonDocument.Parse("{}");
            var result = await _controller.UpdateProfile(10, doc.RootElement);

            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("User not found.", notFound.Value);
        }

        [Fact]
        public async Task UpdateProfile_ShouldReturnBadRequest_WhenUserIsInactive()
        {
            var user = new Driver { IsActive = false };
            _mockRepo.Setup(r => r.GetUserByIdAsync(11)).ReturnsAsync(user);

            using var doc = JsonDocument.Parse("{}");
            var result = await _controller.UpdateProfile(11, doc.RootElement);

            var bad = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("This account is deactivated and cannot be updated.", bad.Value);
        }


        [Fact]
        public async Task UpdateProfile_ShouldReturnOk_WhenDriverUpdatedSuccessfully()
        {
            var driver = new Driver { IsActive = true };
            _mockRepo.Setup(r => r.GetUserByIdAsync(20)).ReturnsAsync(driver);
            _mockRepo.Setup(r => r.UpdateDriverAsync(20, It.IsAny<DriverProfileDTO>())).ReturnsAsync(true);

            var dto = new DriverProfileDTO
            {
                Name = "D",
                Email = "d@ex.com",
                PhoneNumber = 123456789,
                NIF = 911111111,
                DriverLicense = "L",
                Insurance = "I"
            };
            using var doc = JsonDocument.Parse(JsonSerializer.Serialize(dto));

            var result = await _controller.UpdateProfile(20, doc.RootElement);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("Profile updated successfully.", ok.Value);
        }

        [Fact]
        public async Task UpdateProfile_ShouldReturnBadRequest_WhenDriverUpdateFails()
        {
            var driver = new Driver { IsActive = true };
            _mockRepo.Setup(r => r.GetUserByIdAsync(21)).ReturnsAsync(driver);
            _mockRepo.Setup(r => r.UpdateDriverAsync(21, It.IsAny<DriverProfileDTO>())).ReturnsAsync(false);

            var dto = new DriverProfileDTO { Name = "D", PhoneNumber = null, NIF = null };
            using var doc = JsonDocument.Parse(JsonSerializer.Serialize(dto));

            var result = await _controller.UpdateProfile(21, doc.RootElement);

            var bad = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("No valid fields provided or user not found.", bad.Value);
        }

        [Fact]
        public async Task UpdateProfile_ShouldReturnOk_WhenCompanyUpdatedSuccessfully()
        {
            var company = new Company { IsActive = true };
            _mockRepo.Setup(r => r.GetUserByIdAsync(30)).ReturnsAsync(company);
            _mockRepo.Setup(r => r.UpdateCompanyAsync(30, It.IsAny<CompanyProfileDTO>())).ReturnsAsync(true);

            var dto = new CompanyProfileDTO
            {
                Name = "C",
                Email = "c@ex.com",
                PhoneNumber = 123456789,
                NIF = 987654321,
                CompanyName = "Co",
                Address = "addr"
            };
            using var doc = JsonDocument.Parse(JsonSerializer.Serialize(dto));

            var result = await _controller.UpdateProfile(30, doc.RootElement);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("Profile updated successfully.", ok.Value);
        }

        [Fact]
        public async Task UpdateProfile_ShouldReturnBadRequest_WhenCompanyUpdateFails()
        {
            var company = new Company { IsActive = true };
            _mockRepo.Setup(r => r.GetUserByIdAsync(31)).ReturnsAsync(company);
            _mockRepo.Setup(r => r.UpdateCompanyAsync(31, It.IsAny<CompanyProfileDTO>())).ReturnsAsync(false);

            var dto = new CompanyProfileDTO { Name = "C", PhoneNumber = null, NIF = null };
            using var doc = JsonDocument.Parse(JsonSerializer.Serialize(dto));

            var result = await _controller.UpdateProfile(31, doc.RootElement);

            var bad = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("No valid fields provided or user not found.", bad.Value);
        }

        // PUT /api/profile/{id}/desativar
        [Fact]
        public async Task DeactivateUser_ShouldReturnOk_WhenSucceeds()
        {
            _mockRepo.Setup(r => r.DeactivateUserAsync(40)).ReturnsAsync(true);

            var result = await _controller.DeactivateUser(40);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("User deactivated successfully.", ok.Value);
        }

        [Fact]
        public async Task DeactivateUser_ShouldReturnBadRequest_WithMessageOnException()
        {
            _mockRepo.Setup(r => r.DeactivateUserAsync(41)).ThrowsAsync(new Exception("fail"));

            var result = await _controller.DeactivateUser(41);

            var bad = Assert.IsType<BadRequestObjectResult>(result);
            var val = bad.Value;
            var prop = val!.GetType().GetProperty("message");
            Assert.NotNull(prop);
            Assert.Equal("fail", prop.GetValue(val));
        }


        [Theory]
        [InlineData(912345678, true)]
        [InlineData(12345678, false)]      // 8 dígitos
        [InlineData(1234567890, false)]    // 10 dígitos
        [InlineData(-912345678, false)]    // negativo
        public void PhoneNumberValidation_ShouldMatchExpectedRules(int phoneValue, bool expectedValid)
        {
            int? phone = phoneValue;
            var actual = IsValidPhoneNumber(phone);
            Assert.Equal(expectedValid, actual);
        }

        [Fact]
        public void PhoneNumberValidation_NullShouldBeInvalid()
        {
            int? phone = null;
            Assert.False(IsValidPhoneNumber(phone));
        }

        [Theory]
        [InlineData(123456789, true)]
        [InlineData(12345678, false)]
        [InlineData(1234567890, false)]
        [InlineData(-123456789, false)]
        public void NifValidation_ShouldMatchExpectedRules(int nifValue, bool expectedValid)
        {
            int? nif = nifValue;
            var actual = IsValidNif(nif);
            Assert.Equal(expectedValid, actual);
        }

        [Fact]
        public void NifValidation_NullShouldBeInvalid()
        {
            int? nif = null;
            Assert.False(IsValidNif(nif));
        }

        // Helpers de validação
        // - Não nulo
        // - Número positivo
        // - Exatamente 9 dígitos 
        private bool IsValidPhoneNumber(int? phone)
        {
            if (!phone.HasValue) return false;
            if (phone.Value <= 0) return false;
            var s = phone.Value.ToString();
            return s.Length == 9 && s.All(char.IsDigit);
        }

        private bool IsValidNif(int? nif)
        {
            if (!nif.HasValue) return false;
            if (nif.Value <= 0) return false;
            var s = nif.Value.ToString();
            return s.Length == 9 && s.All(char.IsDigit);
        }
    }
}
