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
using System.Text;
using Xunit;

namespace Bid_Go.Tests.Integration.Controllers
{
 /// <summary>
 /// Integration tests for driver registration including document uploads and conflict scenarios.
 /// </summary>
 public class RegisterDriverControllerTests
 {
        private static (RegisterDriverController controller, BidGoDbContext db) Build()
        {
            var options = new DbContextOptionsBuilder<BidGoDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            var db = new BidGoDbContext(options);
            var repo = new RegisterDriverRepository(db);
            var r2 = new TestR2Service();
            var service = new RegisterDriverService(repo, r2);
            var controller = new RegisterDriverController(service);
            return (controller, db);
        }

        private static IFormFile MakeFile(string fileName, string content = "test")
        {
            var bytes = Encoding.UTF8.GetBytes(content);
            var stream = new MemoryStream(bytes);
            return new FormFile(stream,0, stream.Length, fileName, fileName);
        }

        [Fact]
        public async Task Register_ReturnsOk_WhenNewDriver()
        {
            // Arrange
            var (controller, db) = Build();
            var dto = new RegisterDriverDTO { Name = "New Driver", DriverLicense = MakeFile("driverlicense.jpg"), Insurance = MakeFile("insurance.jpg"), Email = "new_driver@test.com", Password = "Abcdef1!", PhoneNumber =911111111, NIF =111222333 };

            // Act
            var result = await controller.Register(dto);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, ok.StatusCode);
            var created = await db.Drivers.FirstOrDefaultAsync(d => d.Email == dto.Email);
            Assert.NotNull(created);
        }

        [Fact]
        public async Task Register_ReturnsConflict_WhenEmailExists()
        {
            var (controller, db) = Build();
            db.Drivers.Add(new Driver
            {
                Name = "Default Driver",
                Email = "driver@test.com",
                Password = BCrypt.Net.BCrypt.HashPassword("123456"),
                PhoneNumber =912345678,
                NIF =123456789,
                DriverLicense = "https://example.com/license.jpg",
                Insurance = "https://example.com/insurance.jpg"
            });
            await db.SaveChangesAsync();

            var dto = new RegisterDriverDTO
            {
                Name = "Dup Email",
                DriverLicense = MakeFile("dl.jpg"),
                Insurance = MakeFile("ins.jpg"),
                Email = "driver@test.com",
                Password = "Abcdef1!",
                PhoneNumber =922222222,
                NIF =222333444
            };

            var result = await controller.Register(dto);
            var conflict = Assert.IsType<ConflictObjectResult>(result);
            Assert.Equal(409, conflict.StatusCode);
        }

        [Fact]
        public async Task Register_ReturnsConflict_WhenPhoneExists()
        {
            var (controller, db) = Build();
            db.Drivers.Add(new Driver
            {
                Name = "Existing",
                Email = "existing_phone@test.com",
                Password = BCrypt.Net.BCrypt.HashPassword("123456"),
                PhoneNumber =912345678,
                NIF =111111111,
                DriverLicense = "x",
                Insurance = "y"
            });
            await db.SaveChangesAsync();

            var dto = new RegisterDriverDTO
            {
                Name = "Dup Phone",
                DriverLicense = MakeFile("dl.jpg"),
                Insurance = MakeFile("ins.jpg"),
                Email = "unique_email_for_phone@test.com",
                Password = "Abcdef1!",
                PhoneNumber =912345678,
                NIF =333444555
            };

            var result = await controller.Register(dto);
            var conflict = Assert.IsType<ConflictObjectResult>(result);
            Assert.Equal(409, conflict.StatusCode);
        }

        [Fact]
        public async Task Register_ReturnsConflict_WhenNifExists()
        {
            var (controller, db) = Build();
            db.Drivers.Add(new Driver
            {
                Name = "Existing",
                Email = "existing_nif@test.com",
                Password = BCrypt.Net.BCrypt.HashPassword("123456"),
                PhoneNumber =900000000,
                NIF =123456789,
                DriverLicense = "x",
                Insurance = "y"
            });
            await db.SaveChangesAsync();

            var dto = new RegisterDriverDTO
            {
                Name = "Dup NIF",
                DriverLicense = MakeFile("dl.jpg"),
                Insurance = MakeFile("ins.jpg"),
                Email = "unique_email_for_nif@test.com",
                Password = "Abcdef1!",
                PhoneNumber =933333333,
                NIF =123456789
            };

            var result = await controller.Register(dto);
            var conflict = Assert.IsType<ConflictObjectResult>(result);
            Assert.Equal(409, conflict.StatusCode);
        }

        [Fact]
        public async Task Register_HashesPassword_OnCreate()
        {
            var (controller, db) = Build();
            var email = "hash_check@test.com";
            var plainPassword = "Abcdef1!";
            var dto = new RegisterDriverDTO
            {
                Name = "Hash Check",
                DriverLicense = MakeFile("dl.jpg"),
                Insurance = MakeFile("ins.jpg"),
                Email = email,
                Password = plainPassword,
                PhoneNumber =944444444,
                NIF =444555666
            };

            var result = await controller.Register(dto);
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, ok.StatusCode);

            var created = await db.Drivers.FirstOrDefaultAsync(d => d.Email == email);
            Assert.NotNull(created);
            Assert.NotEqual(plainPassword, created!.Password);
            Assert.True(BCrypt.Net.BCrypt.Verify(plainPassword, created.Password));
        }

        [Fact]
        public async Task Register_DoesNotUpload_WhenEmailConflict()
        {
            var options = new DbContextOptionsBuilder<BidGoDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            var db = new BidGoDbContext(options);
            var repo = new RegisterDriverRepository(db);
            var recordingR2 = new RecordingR2Service();
            var service = new RegisterDriverService(repo, recordingR2);
            var controller = new RegisterDriverController(service);

            db.Drivers.Add(new Driver
            {
                Name = "Default Driver",
                Email = "driver@test.com",
                Password = BCrypt.Net.BCrypt.HashPassword("123456"),
                PhoneNumber =912345678,
                NIF =123456789,
                DriverLicense = "https://example.com/license.jpg",
                Insurance = "https://example.com/insurance.jpg"
            });
            await db.SaveChangesAsync();

            var dto = new RegisterDriverDTO
            {
                Name = "Dup Email No Upload",
                DriverLicense = MakeFile("dl.jpg"),
                Insurance = MakeFile("ins.jpg"),
                Email = "driver@test.com",
                Password = "Abcdef1!",
                PhoneNumber =966666666,
                NIF =666777888
            };

            var result = await controller.Register(dto);
            var conflict = Assert.IsType<ConflictObjectResult>(result);
            Assert.Equal(409, conflict.StatusCode);
            Assert.Equal(0, recordingR2.UploadCalls);
        }

        private sealed class TestR2Service : ICloudflareR2Service
        {
            public Task DeleteImageAsync(string fileName) => Task.CompletedTask;
            public Task<string> UploadImageAsync(IFormFile file)
                => Task.FromResult($"https://fake.cdn/{file.FileName}");
        }

        private sealed class RecordingR2Service : ICloudflareR2Service
        {
            public int UploadCalls { get; private set; }
            public Task DeleteImageAsync(string fileName) => Task.CompletedTask;
            public Task<string> UploadImageAsync(IFormFile file)
            {
                UploadCalls++;
                return Task.FromResult($"https://fake.cdn/{file.FileName}");
            }
        }
    }
}
