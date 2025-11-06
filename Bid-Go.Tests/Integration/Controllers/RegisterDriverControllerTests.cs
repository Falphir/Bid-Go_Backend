using Bid_Go.Tests.Integration.Utils;
using Bid_Go_Backend.Controllers;
using Bid_Go_Backend.Data.Models.DTOs;
using Bid_Go_Backend.Services.Interfaces;
using Bid_Go_Backend.Services.Register;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using Xunit;

namespace Bid_Go.Tests.Integration.Controllers
{
    public class RegisterDriverControllerTests
    {
        private readonly RegisterDriverController _controller;
        private readonly FakeRegisterDriverRepository _driverRepo;
        private readonly ICloudflareR2Service _r2Service;

        public RegisterDriverControllerTests()
        {
            _driverRepo = new FakeRegisterDriverRepository();
            _r2Service = new FakeCloudflareR2Service();

            var service = new RegisterDriverService(_driverRepo, _r2Service);
            _controller = new RegisterDriverController(service);
        }

        private static IFormFile MakeFile(string fileName, string content = "test")
        {
            var bytes = Encoding.UTF8.GetBytes(content);
            var stream = new MemoryStream(bytes);
            return new FormFile(stream, 0, stream.Length, fileName, fileName);
        }

        [Fact]
        public async Task Register_ReturnsOk_WhenNewDriver()
        {
            // Arrange
            var dto = new RegisterDriverDTO
            {
                Name = "New Driver",
                DriverLicense = MakeFile("driverlicense.jpg"),
                Insurance = MakeFile("insurance.jpg"),
                Email = "new_driver@test.com",
                Password = "Abcdef1!",
                PhoneNumber = 911111111,
                NIF = 111222333
            };

            // Act
            var result = await _controller.Register(dto);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, ok.StatusCode);

            var created = await _driverRepo.GetByEmailAsync(dto.Email);
            Assert.NotNull(created);
            Assert.Equal(dto.Email, created!.Email);
            Assert.Equal(dto.Name, created.Name);
            Assert.Equal(dto.PhoneNumber, created.PhoneNumber);
            Assert.Equal(dto.NIF, created.NIF);
            Assert.Equal("https://fake.cdn/driverlicense.jpg", created.DriverLicense);
            Assert.Equal("https://fake.cdn/insurance.jpg", created.Insurance);
        }

        [Fact]
        public async Task Register_ReturnsConflict_WhenEmailExists()
        {
            // Arrange: existing email in fake repo: driver@test.com
            var dto = new RegisterDriverDTO
            {
                Name = "Dup Email",
                DriverLicense = MakeFile("dl.jpg"),
                Insurance = MakeFile("ins.jpg"),
                Email = "driver@test.com",
                Password = "Abcdef1!",
                PhoneNumber = 922222222,
                NIF = 222333444
            };

            // Act
            var result = await _controller.Register(dto);

            // Assert
            var conflict = Assert.IsType<ConflictObjectResult>(result);
            Assert.Equal(409, conflict.StatusCode);
        }

        [Fact]
        public async Task Register_ReturnsConflict_WhenPhoneExists()
        {
            // Arrange: existing phone in fake repo: 912345678
            var dto = new RegisterDriverDTO
            {
                Name = "Dup Phone",
                DriverLicense = MakeFile("dl.jpg"),
                Insurance = MakeFile("ins.jpg"),
                Email = "unique_email_for_phone@test.com",
                Password = "Abcdef1!",
                PhoneNumber = 912345678, // duplicate
                NIF = 333444555
            };

            // Act
            var result = await _controller.Register(dto);

            // Assert
            var conflict = Assert.IsType<ConflictObjectResult>(result);
            Assert.Equal(409, conflict.StatusCode);
        }

        [Fact]
        public async Task Register_ReturnsConflict_WhenNifExists()
        {
            // Arrange: existing NIF in fake repo: 123456789
            var dto = new RegisterDriverDTO
            {
                Name = "Dup NIF",
                DriverLicense = MakeFile("dl.jpg"),
                Insurance = MakeFile("ins.jpg"),
                Email = "unique_email_for_nif@test.com",
                Password = "Abcdef1!",
                PhoneNumber = 933333333,
                NIF = 123456789 // duplicate
            };

            // Act
            var result = await _controller.Register(dto);

            // Assert
            var conflict = Assert.IsType<ConflictObjectResult>(result);
            Assert.Equal(409, conflict.StatusCode);
        }

        // ----------------------------
        // EXTRA: testes de robustez
        // ----------------------------

        [Fact]
        public async Task Register_HashesPassword_OnCreate()
        {
            // Arrange
            var email = "hash_check@test.com";
            var plainPassword = "Abcdef1!";
            var dto = new RegisterDriverDTO
            {
                Name = "Hash Check",
                DriverLicense = MakeFile("dl.jpg"),
                Insurance = MakeFile("ins.jpg"),
                Email = email,
                Password = plainPassword,
                PhoneNumber = 944444444,
                NIF = 444555666
            };

            // Act
            var result = await _controller.Register(dto);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, ok.StatusCode);

            var created = await _driverRepo.GetByEmailAsync(email);
            Assert.NotNull(created);
            Assert.NotEqual(plainPassword, created!.Password); // não é guardada em claro
            Assert.True(BCrypt.Net.BCrypt.Verify(plainPassword, created.Password)); // confere hash
        }

        [Fact]
        public async Task Register_DoesNotUpload_WhenEmailConflict()
        {
            // Arrange: serviço que regista chamadas
            var recordingR2 = new RecordingCloudflareR2Service();
            var service = new RegisterDriverService(_driverRepo, recordingR2);
            var controller = new RegisterDriverController(service);

            var dto = new RegisterDriverDTO
            {
                Name = "Dup Email No Upload",
                DriverLicense = MakeFile("dl.jpg"),
                Insurance = MakeFile("ins.jpg"),
                Email = "driver@test.com", // já existe
                Password = "Abcdef1!",
                PhoneNumber = 966666666,
                NIF = 666777888
            };

            // Act
            var result = await controller.Register(dto);

            // Assert: conflito
            var conflict = Assert.IsType<ConflictObjectResult>(result);
            Assert.Equal(409, conflict.StatusCode);

            // E não houve uploads ao R2
            Assert.Equal(0, recordingR2.UploadCalls);
        }
    }

    /// <summary>
    /// Serviço que apenas regista quantas vezes o upload foi chamado.
    /// Útil para garantir que não se fazem uploads se houver conflitos de unicidade.
    /// </summary>
    internal sealed class RecordingCloudflareR2Service : ICloudflareR2Service
    {
        public int UploadCalls { get; private set; }

        public Task DeleteImageAsync(string fileName) => Task.CompletedTask;

        public Task<string> UploadImageAsync(IFormFile file)
        {
            UploadCalls++;
            var name = string.IsNullOrWhiteSpace(file?.FileName) ? "unnamed" : file!.FileName;
            return Task.FromResult($"https://fake.cdn/{name}");
        }
    }
}
