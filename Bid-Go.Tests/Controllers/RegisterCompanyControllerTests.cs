using System.Threading.Tasks;
using Bid_Go_Backend.Controllers;
using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.DTOs.CompanyDTOs;
using Bid_Go_Backend.Data.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Bid_Go_Backend.Tests.Controllers
{
    public class RegisterCompanyControllerTests
    {
        private readonly Mock<IRegisterCompanyRepository> _mockRepo;
        private readonly CompanyController _controller;

        public RegisterCompanyControllerTests()
        {
            _mockRepo = new Mock<IRegisterCompanyRepository>();
            _controller = new CompanyController(_mockRepo.Object);
        }

        // Teste: valida que o controlador respeita ModelState inválido.
        // Setup: força um erro de validação em ModelState.
        // Act: chama Register com um DTO vazio.
        // Assert: espera BadRequestObjectResult (400).
        [Fact]
        public async Task Register_ReturnsBadRequest_WhenModelStateInvalid()
        {
            _controller.ModelState.AddModelError("Email", "Required");

            var dto = new RegisterCompanyDTO();
            var result = await _controller.Register(dto);

            Assert.IsType<BadRequestObjectResult>(result);
        }


        // Teste: quando o email já existe deve retornar Conflict.
        // Setup: o repositório mock devolve uma Company para o email testado.
        // Act: chama Register com DTO contendo o mesmo email.
        // Assert: verifica ConflictObjectResult e mensagem de conflito de email.
        [Fact]
        public async Task Register_ReturnsConflict_WhenEmailAlreadyRegistered()
        {
            var existing = new Company { Id = 1, Email = "a@b.com" };
            _mockRepo.Setup(r => r.GetByEmailAsync("a@b.com")).ReturnsAsync(existing);

            var dto = new RegisterCompanyDTO { Name = "N", CompanyName = "C", Address = "A", Email = "a@b.com", Password = "P@ssw0rd!", PhoneNumber = 912345678, NIF = 123456789 };
            var result = await _controller.Register(dto);

            var conflict = Assert.IsType<ConflictObjectResult>(result);
            var val = conflict.Value;
            var prop = val!.GetType().GetProperty("message");
            Assert.NotNull(prop);
            Assert.Equal("Email is already registered.", prop.GetValue(val));
        }


        // Teste: quando o número de telefone já existe deve retornar Conflict.
        // Setup: garante null para GetByEmailAsync e configura GetByPhoneAsync para devolver uma Company.
        // Act: chama Register com DTO que contém o número de telefone em conflito.
        // Assert: verifica ConflictObjectResult e mensagem de conflito de telefone.
        [Fact]
        public async Task Register_ReturnsConflict_WhenPhoneAlreadyRegistered()
        {
            _mockRepo.Setup(r => r.GetByEmailAsync("x@y.com")).ReturnsAsync((Company?)null);
            var existing = new Company { Id = 2, PhoneNumber = 912345678 };
            _mockRepo.Setup(r => r.GetByPhoneAsync(912345678)).ReturnsAsync(existing);

            var dto = new RegisterCompanyDTO { Name = "N", CompanyName = "C", Address = "A", Email = "x@y.com", Password = "P@ssw0rd!", PhoneNumber = 912345678, NIF = 123456789 };
            var result = await _controller.Register(dto);

            var conflict = Assert.IsType<ConflictObjectResult>(result);
            var val = conflict.Value;
            var prop = val!.GetType().GetProperty("message");
            Assert.NotNull(prop);
            Assert.Equal("Phone number is already registered.", prop.GetValue(val));
        }


        // Teste: quando o NIF já existe deve retornar Conflict.
        // Setup: garante null para GetByEmailAsync e GetByPhoneAsync; configura GetByNIFAsync para devolver uma Company.
        // Act: chama Register com DTO que contém o NIF em conflito.
        // Assert: verifica ConflictObjectResult e mensagem de conflito de NIF.
        [Fact]
        public async Task Register_ReturnsConflict_WhenNifAlreadyRegistered()
        {
            _mockRepo.Setup(r => r.GetByEmailAsync("u@v.com")).ReturnsAsync((Company?)null);
            _mockRepo.Setup(r => r.GetByPhoneAsync(912345679)).ReturnsAsync((Company?)null);
            var existing = new Company { Id = 3, NIF = 123456789 };
            _mockRepo.Setup(r => r.GetByNIFAsync(123456789)).ReturnsAsync(existing);

            var dto = new RegisterCompanyDTO { Name = "N", CompanyName = "C", Address = "A", Email = "u@v.com", Password = "P@ssw0rd!", PhoneNumber = 912345679, NIF = 123456789 };
            var result = await _controller.Register(dto);

            var conflict = Assert.IsType<ConflictObjectResult>(result);
            var val = conflict.Value;
            var prop = val!.GetType().GetProperty("message");
            Assert.NotNull(prop);
            Assert.Equal("Tax ID (NIF) is already registered.", prop.GetValue(val));
        }


        // Teste: fluxo bem sucedido de criação de empresa.
        // Setup:
        //  - garante que não há conflitos (GetByEmailAsync/GetByPhoneAsync/GetByNIFAsync retornam null),
        //  - mock do CreateAsync captura a Company enviada e simula persistência atribuindo um Id.
        // Act: chama Register com DTO válido.
        // Assert:
        //  - verifica OkObjectResult e mensagem de sucesso,
        //  - valida campos retornados do objeto anónimo `company` (Id, Email, CompanyName),
        //  - garante que a senha passada ao repositório está hasheada (não igual ao plaintext),
        //  - garante que CreateAsync foi chamado exatamente uma vez.
        [Fact]
        public async Task Register_ReturnsOk_WhenCreationSucceeds()
        {
            _mockRepo.Setup(r => r.GetByEmailAsync("new@co.com")).ReturnsAsync((Company?)null);
            _mockRepo.Setup(r => r.GetByPhoneAsync(912345680)).ReturnsAsync((Company?)null);
            _mockRepo.Setup(r => r.GetByNIFAsync(987654321)).ReturnsAsync((Company?)null);

            Company? captured = null;
            _mockRepo.Setup(r => r.CreateAsync(It.IsAny<Company>()))
                     .Callback<Company>(c => captured = c)
                     .ReturnsAsync((Company c) => { c.Id = 99; return c; });

            var dto = new RegisterCompanyDTO
            {
                Name = "Nome",
                CompanyName = "Empresa",
                Address = "Rua 1",
                Email = "new@co.com",
                Password = "StrongP@ss1",
                PhoneNumber = 912345680,
                NIF = 987654321
            };

            var result = await _controller.Register(dto);

            var ok = Assert.IsType<OkObjectResult>(result);
            var val = ok.Value!;
            var propMsg = val.GetType().GetProperty("message");
            var propCompany = val.GetType().GetProperty("company");
            Assert.NotNull(propMsg);
            Assert.NotNull(propCompany);
            Assert.Equal("Company account created successfully.", propMsg.GetValue(val));

            var returnedCompany = propCompany!.GetValue(val);
            var propId = returnedCompany!.GetType().GetProperty("Id");
            var propEmail = returnedCompany.GetType().GetProperty("Email");
            var propCompanyName = returnedCompany.GetType().GetProperty("CompanyName");
            Assert.Equal(99, propId!.GetValue(returnedCompany));
            Assert.Equal(dto.Email, propEmail!.GetValue(returnedCompany));
            Assert.Equal(dto.CompanyName, propCompanyName!.GetValue(returnedCompany));

            // Garantir que o repository recebeu uma empresa e que a senha foi criptografada (e não está em texto normal)
            Assert.NotNull(captured);
            Assert.NotEqual(dto.Password, captured!.Password);
            _mockRepo.Verify(r => r.CreateAsync(It.IsAny<Company>()), Times.Once);
        }
    }
}