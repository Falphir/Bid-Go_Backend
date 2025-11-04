using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.DTOs.CompanyDTOs;
using Bid_Go_Backend.Data.Repositories.Interfaces;
using Bid_Go_Backend.Services.Register;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Bid_Go.Tests.Services
{
    public class RegisterCompanyServiceTests
    {
        private static RegisterCompanyDTO MakeDto() => new RegisterCompanyDTO
        {
            Name = "Acme User",
            CompanyName = "Acme Ltd",
            Address = "Rua 1",
            Email = "acme@example.com",
            Password = "MyP@ssw0rd!",
            PhoneNumber = 932000111,
            NIF = 123456789
        };

        [Fact]
        public async Task RegisterAsync_ShouldReturnSuccess_WhenAllUnique()
        {
            // Arrange
            var repo = new Mock<IRegisterCompanyRepository>();
            repo.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((Company?)null);
            repo.Setup(r => r.GetByPhoneAsync(It.IsAny<int>())).ReturnsAsync((Company?)null);
            repo.Setup(r => r.GetByNIFAsync(It.IsAny<int>())).ReturnsAsync((Company?)null);

            Company? capturedCompany = null;
            repo.Setup(r => r.CreateAsync(It.IsAny<Company>()))
                .Callback<Company>(c => { c.Id = 42; capturedCompany = c; })
                .ReturnsAsync((Company c) => c);

            var service = new RegisterCompanyService(repo.Object);
            var dto = MakeDto();

            // Act
            var (success, error, company) = await service.RegisterAsync(dto);

            // Assert
            Assert.True(success);
            Assert.Null(error);
            Assert.NotNull(company);
            Assert.Equal(42, company!.Id);
            Assert.Equal(dto.Email, company.Email);
            Assert.Equal(dto.PhoneNumber, company.PhoneNumber);
            Assert.Equal(dto.NIF, company.NIF);

            Assert.NotNull(capturedCompany);
            Assert.NotEqual(dto.Password, capturedCompany!.Password);
            Assert.False(string.IsNullOrWhiteSpace(capturedCompany.Password));

            repo.Verify(r => r.CreateAsync(It.IsAny<Company>()), Times.Once);
        }

        [Fact]
        public async Task RegisterAsync_ShouldFail_WhenEmailExists()
        {
            // Arrange
            var repo = new Mock<IRegisterCompanyRepository>();
            repo.Setup(r => r.GetByEmailAsync(It.IsAny<string>()))
                .ReturnsAsync(new Company { Email = "acme@example.com" });

            var service = new RegisterCompanyService(repo.Object);
            var dto = MakeDto();

            // Act
            var (success, error, company) = await service.RegisterAsync(dto);

            // Assert
            Assert.False(success);
            Assert.Equal("EMAIL_EXISTS", error);
            Assert.Null(company);
            repo.Verify(r => r.CreateAsync(It.IsAny<Company>()), Times.Never);
        }

        [Fact]
        public async Task RegisterAsync_ShouldFail_WhenPhoneExists()
        {
            // Arrange
            var repo = new Mock<IRegisterCompanyRepository>();
            repo.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((Company?)null);
            repo.Setup(r => r.GetByPhoneAsync(It.IsAny<int>()))
                .ReturnsAsync(new Company { PhoneNumber = 932000111 });

            var service = new RegisterCompanyService(repo.Object);
            var dto = MakeDto();

            // Act
            var (success, error, company) = await service.RegisterAsync(dto);

            // Assert
            Assert.False(success);
            Assert.Equal("PHONE_EXISTS", error);
            Assert.Null(company);
            repo.Verify(r => r.CreateAsync(It.IsAny<Company>()), Times.Never);
        }

        [Fact]
        public async Task RegisterAsync_ShouldFail_WhenNifExists()
        {
            // Arrange
            var repo = new Mock<IRegisterCompanyRepository>();
            repo.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((Company?)null);
            repo.Setup(r => r.GetByPhoneAsync(It.IsAny<int>())).ReturnsAsync((Company?)null);
            repo.Setup(r => r.GetByNIFAsync(It.IsAny<int>()))
                .ReturnsAsync(new Company { NIF = 123456789 });

            var service = new RegisterCompanyService(repo.Object);
            var dto = MakeDto();

            // Act
            var (success, error, company) = await service.RegisterAsync(dto);

            // Assert
            Assert.False(success);
            Assert.Equal("NIF_EXISTS", error);
            Assert.Null(company);
            repo.Verify(r => r.CreateAsync(It.IsAny<Company>()), Times.Never);
        }
    }
}
