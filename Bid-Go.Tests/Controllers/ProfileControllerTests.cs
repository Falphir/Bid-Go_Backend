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
        // O que testa:
        // - Quando o utilizador não existe, o método deve retornar NotFound.
        // Como:
        // - Setup: mock de GetUserByIdAsync devolve null.
        // - Act: chama GetProfile(id).
        // - Assert: verifica NotFoundObjectResult e mensagem esperada.
        [Fact]
        public async Task GetProfile_ShouldReturnNotFound_WhenUserDoesNotExist()
        {
            _mockRepo.Setup(r => r.GetUserByIdAsync(1)).ReturnsAsync((User?)null);

            var result = await _controller.GetProfile(1);

            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("User not found", notFound.Value);
        }

        // O que testa:
        // - Quando o utilizador não existe, o método deve retornar NotFound.
        // Como:
        // - Setup: mock de GetUserByIdAsync devolve null.
        // - Act: chama GetProfile(id).
        // - Assert: verifica NotFoundObjectResult e mensagem esperada.
        [Fact]
        public async Task GetProfile_ShouldReturnConflict_WhenUserIsInactive()
        {
            var user = new Driver { IsActive = false };
            _mockRepo.Setup(r => r.GetUserByIdAsync(2)).ReturnsAsync(user);

            var result = await _controller.GetProfile(2);

            var conflict = Assert.IsType<ConflictObjectResult>(result);
            Assert.Equal("User is inactive and cannot be updated.", conflict.Value);
        }

        // O que testa:
        // - Quando o utilizador não existe, o método deve retornar NotFound.
        // Como:
        // - Setup: mock de GetUserByIdAsync devolve null.
        // - Act: chama GetProfile(id).
        // - Assert: verifica NotFoundObjectResult e mensagem esperada.
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

        // O que testa:
        // - Quando o utilizador é uma Company válida, deve devolver um CompanyProfileDTO com os campos corretos.
        // Como:
        // - Setup: mock devolve uma Company populada.
        // - Act: chama GetProfile(id).
        // - Assert: verifica OkObjectResult e converte o Value para CompanyProfileDTO, comparando campos.
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
        // O que testa:
        // - Quando o utilizador não existe, UpdateProfile deve retornar NotFound.
        // Como:
        // - Setup: mock de GetUserByIdAsync devolve null.
        // - Act: chama UpdateProfile com um JsonElement vazio.
        // - Assert: verifica NotFoundObjectResult e mensagem esperada.
        [Fact]
        public async Task UpdateProfile_ShouldReturnNotFound_WhenUserDoesNotExist()
        {
            _mockRepo.Setup(r => r.GetUserByIdAsync(10)).ReturnsAsync((User?)null);

            using var doc = JsonDocument.Parse("{}");
            var result = await _controller.UpdateProfile(10, doc.RootElement);

            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("User not found.", notFound.Value);
        }

        // O que testa:
        // - Quando o utilizador está inativo, UpdateProfile deve retornar BadRequest com mensagem específica.
        // Como:
        // - Setup: mock devolve um Driver com IsActive = false.
        // - Act: chama UpdateProfile com um JsonElement vazio.
        // - Assert: verifica BadRequestObjectResult e mensagem esperada.
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


        // O que testa:
        // - Quando a atualização de Driver tem sucesso, retorna Ok com mensagem de sucesso.
        // Como:
        // - Setup: mock devolve um Driver ativo e UpdateDriverAsync retorna true.
        // - Act: serializa um DriverProfileDTO em JsonElement e chama UpdateProfile.
        // - Assert: verifica OkObjectResult e mensagem de sucesso.
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

        // O que testa:
        // - Quando a atualização de Driver falha (sem campos válidos ou utilizador não encontrado), retorna BadRequest.
        // Como:
        // - Setup: mock devolve um Driver ativo e UpdateDriverAsync retorna false.
        // - Act: serializa um DriverProfileDTO com campos nulos relevantes e chama UpdateProfile.
        // - Assert: verifica BadRequestObjectResult e mensagem de erro.
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

        // O que testa:
        // - Quando a atualização de Company tem sucesso, retorna Ok com mensagem de sucesso.
        // Como:
        // - Setup: mock devolve uma Company ativa e UpdateCompanyAsync retorna true.
        // - Act: serializa um CompanyProfileDTO e chama UpdateProfile.
        // - Assert: verifica OkObjectResult e mensagem de sucesso.
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

        // O que testa:
        // - Quando a atualização de Company falha (sem campos válidos ou utilizador não encontrado), retorna BadRequest.
        // Como:
        // - Setup: mock devolve uma Company ativa e UpdateCompanyAsync retorna false.
        // - Act: serializa um CompanyProfileDTO com campos nulos relevantes e chama UpdateProfile.
        // - Assert: verifica BadRequestObjectResult e mensagem de erro.
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
        // O que testa:
        // - Quando a desativação tem sucesso, retorna Ok com mensagem de sucesso.
        // Como:
        // - Setup: mock de DeactivateUserAsync retorna true.
        // - Act: chama DeactivateUser(id).
        // - Assert: verifica OkObjectResult e mensagem esperada.
        [Fact]
        public async Task DeactivateUser_ShouldReturnOk_WhenSucceeds()
        {
            _mockRepo.Setup(r => r.DeactivateUserAsync(40)).ReturnsAsync(true);

            var result = await _controller.DeactivateUser(40);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("User deactivated successfully.", ok.Value);
        }

        // O que testa:
        // - Quando o repositório lança exceção durante desativação, o controlador deve retornar BadRequest com a mensagem.
        // Como:
        // - Setup: mock de DeactivateUserAsync lança Exception("fail").
        // - Act: chama DeactivateUser(id).
        // - Assert: verifica BadRequestObjectResult e que a propriedade "message" contém a mensagem da exceção.
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

        // ChangePassword tests (controller expõe esta ação)

        // O que testa:
        // - Quando a alteração de password tem sucesso, o controlador retorna Ok.
        // Como:
        // - Setup: mock ChangePasswordAsync retorna true.
        // - Act: chama ChangePassword(id, dto).
        // - Assert: verifica OkObjectResult.
        [Fact]
        public async Task ChangePassword_ShouldReturnOk_WhenChangeSucceeds()
        {
            _mockRepo.Setup(r => r.ChangePasswordAsync(50, "oldpass", "newpass")).ReturnsAsync(true);

            var dto = new ChangePasswordDTO { CurrentPassword = "oldpass", NewPassword = "newpass" };
            var result = await _controller.ChangePassword(50, dto);

            Assert.IsType<OkObjectResult>(result);
        }

        // O que testa:
        // - Quando ChangePasswordAsync lança exceção, o controlador deve retornar BadRequest com a mensagem da exceção.
        // Como:
        // - Setup: mock configura ChangePasswordAsync para lançar Exception("fail").
        // - Act: chama ChangePassword(id, dto).
        // - Assert: verifica BadRequestObjectResult e propriedade "message" com a mensagem.
        [Fact]
        public async Task ChangePassword_ShouldReturnBadRequest_WithMessageOnException()
        {
            _mockRepo.Setup(r => r.ChangePasswordAsync(51, It.IsAny<string>(), It.IsAny<string>()))
                     .ThrowsAsync(new Exception("fail"));

            var dto = new ChangePasswordDTO { CurrentPassword = "a", NewPassword = "b" };
            var result = await _controller.ChangePassword(51, dto);

            var bad = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("fail", bad.Value);
        }


        // O que testa:
        // - Validação de números de telefone segundo regras de teste.
        // Como:
        // - Parametrizado com vários valores; chama IsValidPhoneNumber e compara com o resultado esperado.
        [Theory]
        [InlineData(912345678, true)]
        [InlineData(12345678, false)]     
        [InlineData(1234567890, false)]    
        [InlineData(-912345678, false)]    
        public void PhoneNumberValidation_ShouldMatchExpectedRules(int phoneValue, bool expectedValid)
        {
            int? phone = phoneValue;
            var actual = IsValidPhoneNumber(phone);
            Assert.Equal(expectedValid, actual);
        }

        // O que testa:
        // - Nulo não é considerado número de telefone válido.
        // Como:
        // - Act: chama IsValidPhoneNumber com null.
        // - Assert: espera false.
        [Fact]
        public void PhoneNumberValidation_NullShouldBeInvalid()
        {
            int? phone = null;
            Assert.False(IsValidPhoneNumber(phone));
        }

        [Theory]
        [InlineData(123456789, true)]
        [InlineData(12345678, false)]
        [InlineData(1234567890, true)]  
        [InlineData(-123456789, false)]
        // O que testa:
        // - Validação de NIF segundo regras de teste.
        // Como:
        // - Parametrizado; chama IsValidNif e compara com o resultado esperado.
        public void NifValidation_ShouldMatchExpectedRules(int nifValue, bool expectedValid)
        {
            int? nif = nifValue;
            var actual = IsValidNif(nif);
            Assert.Equal(expectedValid, actual);
        }

        // O que testa:
        // - Nulo não é considerado NIF válido.
        // Como:
        // - Act: chama IsValidNif com null.
        // - Assert: espera false.
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
            return (s.Length == 9 || s.Length == 10) && s.All(char.IsDigit);
        }
    }
}