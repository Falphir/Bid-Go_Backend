using Bid_Go_Backend.Controllers;
using Bid_Go_Backend.Data;
using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.DTOs.CompanyDTOs;
using Bid_Go_Backend.Repositories.Register;
using Bid_Go_Backend.Services.Register;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Bid_Go.Tests.Integration.Controllers
{
 public class RegisterCompanyControllerTests
 {
 private static (RegisterCompanyController controller, BidGoDbContext db) Build()
 {
 var options = new DbContextOptionsBuilder<BidGoDbContext>()
 .UseInMemoryDatabase(Guid.NewGuid().ToString())
 .Options;
 var db = new BidGoDbContext(options);
 var repo = new RegisterCompanyRepository(db);
 var service = new RegisterCompanyService(repo);
 var controller = new RegisterCompanyController(service);
 return (controller, db);
 }

 [Fact]
 public async Task Register_ReturnsOk_WhenNewCompany()
 {
 var (controller, db) = Build();
 var dto = new RegisterCompanyDTO
 {
 Name = "New Admin",
 CompanyName = "New Co",
 Address = "Rua X123",
 Email = "new_company@test.com",
 Password = "Abcdef1!",
 PhoneNumber =988888888,
 NIF =555666777
 };

 var result = await controller.Register(dto);
 var ok = Assert.IsType<OkObjectResult>(result);
 Assert.Equal(200, ok.StatusCode);

 var created = await db.Companies.FirstOrDefaultAsync(c => c.Email == dto.Email);
 Assert.NotNull(created);
 Assert.Equal(dto.Email, created!.Email);
 Assert.Equal(dto.CompanyName, created.CompanyName);
 Assert.Equal(dto.PhoneNumber, created.PhoneNumber);
 Assert.Equal(dto.NIF, created.NIF);
 }

 [Fact]
 public async Task Register_ReturnsConflict_WhenEmailExists()
 {
 var (controller, db) = Build();
 db.Companies.Add(new Company
 {
 Name = "Default Admin",
 CompanyName = "BidGo Test Company",
 Address = "Rua de Teste123",
 Email = "company@test.com",
 Password = BCrypt.Net.BCrypt.HashPassword("123456"),
 PhoneNumber =987654321,
 NIF =999888777
 });
 await db.SaveChangesAsync();

 var dto = new RegisterCompanyDTO
 {
 Name = "Dup Admin",
 CompanyName = "Dup Co",
 Address = "Rua Y456",
 Email = "company@test.com",
 Password = "Abcdef1!",
 PhoneNumber =977777777,
 NIF =111222333
 };

 var result = await controller.Register(dto);
 var conflict = Assert.IsType<ConflictObjectResult>(result);
 Assert.Equal(409, conflict.StatusCode);
 }

 [Fact]
 public async Task Register_ReturnsConflict_WhenPhoneExists()
 {
 var (controller, db) = Build();
 db.Companies.Add(new Company
 {
 Name = "Existing",
 CompanyName = "X",
 Address = "Y",
 Email = "existing_phone@test.com",
 Password = BCrypt.Net.BCrypt.HashPassword("123456"),
 PhoneNumber =987654321,
 NIF =111111111
 });
 await db.SaveChangesAsync();

 var dto = new RegisterCompanyDTO
 {
 Name = "Dup Phone",
 CompanyName = "Dup Co",
 Address = "Rua Y456",
 Email = "unique_for_phone@test.com",
 Password = "Abcdef1!",
 PhoneNumber =987654321,
 NIF =111222333
 };

 var result = await controller.Register(dto);
 var conflict = Assert.IsType<ConflictObjectResult>(result);
 Assert.Equal(409, conflict.StatusCode);
 }

 [Fact]
 public async Task Register_ReturnsConflict_WhenNifExists()
 {
 var (controller, db) = Build();
 db.Companies.Add(new Company
 {
 Name = "Existing",
 CompanyName = "X",
 Address = "Y",
 Email = "existing_nif@test.com",
 Password = BCrypt.Net.BCrypt.HashPassword("123456"),
 PhoneNumber =911111111,
 NIF =999888777
 });
 await db.SaveChangesAsync();

 var dto = new RegisterCompanyDTO
 {
 Name = "Dup NIF",
 CompanyName = "Dup Co",
 Address = "Rua Y456",
 Email = "unique_for_nif@test.com",
 Password = "Abcdef1!",
 PhoneNumber =944444444,
 NIF =999888777
 };

 var result = await controller.Register(dto);
 var conflict = Assert.IsType<ConflictObjectResult>(result);
 Assert.Equal(409, conflict.StatusCode);
 }
 }
}
