using Bid_Go.Tests.Integration.Utils;
using Bid_Go_Backend.Controllers;
using Bid_Go_Backend.Data.Models.DTOs.CompanyDTOs;
using Bid_Go_Backend.Services.Register;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Bid_Go.Tests.Integration.Controllers
{
 public class RegisterCompanyControllerTests
 {
 private readonly RegisterCompanyController _controller;
 private readonly FakeRegisterCompanyRepository _companyRepo;

 public RegisterCompanyControllerTests()
 {
 _companyRepo = new FakeRegisterCompanyRepository();
 var service = new RegisterCompanyService(_companyRepo);
 _controller = new RegisterCompanyController(service);
 }

 [Fact]
 public async Task Register_ReturnsOk_WhenNewCompany()
 {
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

 var result = await _controller.Register(dto);

 var ok = Assert.IsType<OkObjectResult>(result);
 Assert.Equal(200, ok.StatusCode);
 var created = await _companyRepo.GetByEmailAsync(dto.Email);
 Assert.NotNull(created);
 Assert.Equal(dto.Email, created!.Email);
 Assert.Equal(dto.CompanyName, created.CompanyName);
 Assert.Equal(dto.PhoneNumber, created.PhoneNumber);
 Assert.Equal(dto.NIF, created.NIF);
 }

 [Fact]
 public async Task Register_ReturnsConflict_WhenEmailExists()
 {
 var dto = new RegisterCompanyDTO
 {
 Name = "Dup Admin",
 CompanyName = "Dup Co",
 Address = "Rua Y456",
 Email = "company@test.com", // exists from fake repo seed
 Password = "Abcdef1!",
 PhoneNumber =977777777,
 NIF =111222333
 };

 var result = await _controller.Register(dto);

 var conflict = Assert.IsType<ConflictObjectResult>(result);
 Assert.Equal(409, conflict.StatusCode);
 }

 [Fact]
 public async Task Register_ReturnsConflict_WhenPhoneExists()
 {
 var dto = new RegisterCompanyDTO
 {
 Name = "Dup Phone",
 CompanyName = "Dup Co",
 Address = "Rua Y456",
 Email = "unique_for_phone@test.com",
 Password = "Abcdef1!",
 PhoneNumber =987654321, // exists
 NIF =111222333
 };

 var result = await _controller.Register(dto);

 var conflict = Assert.IsType<ConflictObjectResult>(result);
 Assert.Equal(409, conflict.StatusCode);
 }

 [Fact]
 public async Task Register_ReturnsConflict_WhenNifExists()
 {
 var dto = new RegisterCompanyDTO
 {
 Name = "Dup NIF",
 CompanyName = "Dup Co",
 Address = "Rua Y456",
 Email = "unique_for_nif@test.com",
 Password = "Abcdef1!",
 PhoneNumber =944444444,
 NIF =999888777 // exists
 };

 var result = await _controller.Register(dto);

 var conflict = Assert.IsType<ConflictObjectResult>(result);
 Assert.Equal(409, conflict.StatusCode);
 }
 }
}
