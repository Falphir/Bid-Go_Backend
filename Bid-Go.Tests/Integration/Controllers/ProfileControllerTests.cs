using Bid_Go_Backend.Controllers;
using Bid_Go_Backend.Data;
using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.DTOs;
using Bid_Go_Backend.Repositories.Profile;
using Bid_Go_Backend.Services.Interfaces;
using Bid_Go_Backend.Services.Profile;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text;
using Xunit;

namespace Bid_Go.Tests.Integration.Controllers
{
 public class ProfileControllerTests
 {
 private static (ProfileController controller, BidGoDbContext db) BuildAs(string role, int userId)
 {
 var options = new DbContextOptionsBuilder<BidGoDbContext>()
 .UseInMemoryDatabase(Guid.NewGuid().ToString())
 .Options;
 var db = new BidGoDbContext(options);
 var repo = new ProfileRepository(db);
 var r2 = new TestR2();
 var service = new ProfileService(repo, r2);
 var controller = new ProfileController(service);
 var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
 {
 new Claim("userId", userId.ToString()),
 new Claim("userType", role)
 }, "TestAuth"));
 controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = user } };
 return (controller, db);
 }

 private static IFormFile MakeFile(string name="file.jpg")
 {
 var bytes = Encoding.UTF8.GetBytes("x");
 return new FormFile(new MemoryStream(bytes),0,bytes.Length,name,name);
 }

 [Fact]
 public async Task GetProfile_Driver_ReturnsDriverProfileDto()
 {
 var (controller, db) = BuildAs("Driver", userId:1);
 var driver = new Driver
 {
 Name = "D",
 Email = "d@x.com",
 Password = BCrypt.Net.BCrypt.HashPassword("123456Ab!"),
 PhoneNumber =911111111,
 NIF =123456789,
 DriverLicense = "DL",
 Insurance = "INS",
 IsActive = true
 };
 db.Users.Add(driver);
 await db.SaveChangesAsync();
 controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
 {
 new Claim("userId", driver.Id.ToString()),
 new Claim("userType", "Driver")
 }, "TestAuth"));
 var result = await controller.GetProfile(driver.Id);
 var ok = Assert.IsType<OkObjectResult>(result);
 var dto = Assert.IsType<DriverProfileDTO>(ok.Value);
 Assert.Equal(driver.Email, dto.Email);
 }

 [Fact]
 public async Task GetProfile_Company_ReturnsCompanyProfileDto()
 {
 var (controller, db) = BuildAs("Company", userId:1);
 var company = new Company
 {
 Name = "C",
 CompanyName = "CC",
 Address = "A",
 Email = "c@x.com",
 Password = BCrypt.Net.BCrypt.HashPassword("123456Ab!"),
 PhoneNumber =922222222,
 NIF =987654321,
 IsActive = true
 };
 db.Users.Add(company);
 await db.SaveChangesAsync();
 controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
 {
 new Claim("userId", company.Id.ToString()),
 new Claim("userType", "Company")
 }, "TestAuth"));
 var result = await controller.GetProfile(company.Id);
 var ok = Assert.IsType<OkObjectResult>(result);
 var dto = Assert.IsType<CompanyProfileDTO>(ok.Value);
 Assert.Equal(company.Email, dto.Email);
 }

 [Fact]
 public async Task UpdateDriverProfile_UpdatesFields_AndUploadsFiles()
 {
 var (controller, db) = BuildAs("Driver", userId:1);
 var driver = new Driver
 {
 Name = "D",
 Email = "d@x.com",
 Password = BCrypt.Net.BCrypt.HashPassword("123456Ab!"),
 PhoneNumber =911111111,
 NIF =123456789,
 DriverLicense = "prevDL",
 Insurance = "prevINS",
 IsActive = true
 };
 db.Users.Add(driver);
 await db.SaveChangesAsync();
 controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
 {
 new Claim("userId", driver.Id.ToString()),
 new Claim("userType", "Driver")
 }, "TestAuth"));
 var dto = new DriverProfileUpdateDTO
 {
 Name = "New Name",
 Email = "new@x.com",
 PhoneNumber =933333333,
 NIF =111222333,
 DriverLicense = MakeFile("dl.png"),
 Insurance = MakeFile("ins.png")
 };
 var result = await controller.UpdateDriverProfile(driver.Id, dto);
 var ok = Assert.IsType<OkObjectResult>(result);
 var fromDb = await db.Drivers.FindAsync(driver.Id);
 Assert.Equal("New Name", fromDb!.Name);
 Assert.Equal("new@x.com", fromDb.Email);
 Assert.Equal(933333333, fromDb.PhoneNumber);
 Assert.Equal(111222333, fromDb.NIF);
 Assert.Equal("https://r2/dl.png", fromDb.DriverLicense);
 Assert.Equal("https://r2/ins.png", fromDb.Insurance);
 }

 [Fact]
 public async Task UpdateCompanyProfile_UpdatesFields()
 {
 var (controller, db) = BuildAs("Company", userId:1);
 var company = new Company
 {
 Name = "C",
 CompanyName = "CC",
 Address = "A",
 Email = "c@x.com",
 Password = BCrypt.Net.BCrypt.HashPassword("123456Ab!"),
 PhoneNumber =922222222,
 NIF =987654321,
 IsActive = true
 };
 db.Users.Add(company);
 await db.SaveChangesAsync();
 controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
 {
 new Claim("userId", company.Id.ToString()),
 new Claim("userType", "Company")
 }, "TestAuth"));
 var dto = new CompanyProfileDTO
 {
 Name = "C2",
 CompanyName = "CC2",
 Address = "A2",
 Email = "c2@x.com",
 PhoneNumber =955555555,
 NIF =123123123
 };
 var result = await controller.UpdateCompanyProfile(company.Id, dto);
 var ok = Assert.IsType<OkObjectResult>(result);
 var fromDb = await db.Companies.FindAsync(company.Id);
 Assert.Equal("C2", fromDb!.Name);
 Assert.Equal("CC2", fromDb.CompanyName);
 Assert.Equal("A2", fromDb.Address);
 Assert.Equal("c2@x.com", fromDb.Email);
 Assert.Equal(955555555, fromDb.PhoneNumber);
 Assert.Equal(123123123, fromDb.NIF);
 }

 [Fact]
 public async Task ChangePassword_Changes_WhenCurrentMatches()
 {
 var (controller, db) = BuildAs("Driver", userId:1);
 var driver = new Driver
 {
 Name = "D",
 Email = "d@x.com",
 Password = BCrypt.Net.BCrypt.HashPassword("OldPass1!"),
 PhoneNumber =911111111,
 NIF =123456789,
 IsActive = true
 };
 db.Users.Add(driver);
 await db.SaveChangesAsync();
 controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
 {
 new Claim("userId", driver.Id.ToString()),
 new Claim("userType", "Driver")
 }, "TestAuth"));
 var result = await controller.ChangePassword(driver.Id, new ChangePasswordDTO { CurrentPassword = "OldPass1!", NewPassword = "NewPass1!" });
 var ok = Assert.IsType<OkObjectResult>(result);
 var fromDb = await db.Users.FindAsync(driver.Id);
 Assert.True(BCrypt.Net.BCrypt.Verify("NewPass1!", fromDb!.Password));
 }

 [Fact]
 public async Task DeactivateUser_SetsInactive_WhenNoActiveDeps()
 {
 var (controller, db) = BuildAs("Driver", userId:1);
 var driver = new Driver
 {
 Name = "D",
 Email = "d@x.com",
 Password = BCrypt.Net.BCrypt.HashPassword("123456Ab!"),
 PhoneNumber =911111111,
 NIF =123456789,
 IsActive = true
 };
 db.Users.Add(driver);
 await db.SaveChangesAsync();
 controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
 {
 new Claim("userId", driver.Id.ToString()),
 new Claim("userType", "Driver")
 }, "TestAuth"));
 var result = await controller.DeactivateUser(driver.Id);
 var ok = Assert.IsType<OkObjectResult>(result);
 var fromDb = await db.Users.FindAsync(driver.Id);
 Assert.False(fromDb!.IsActive);
 }

 private sealed class TestR2 : ICloudflareR2Service
 {
 public Task DeleteImageAsync(string fileName) => Task.CompletedTask;
 public Task<string> UploadImageAsync(IFormFile file) => Task.FromResult($"https://r2/{file.FileName}");
 }
 }
}
