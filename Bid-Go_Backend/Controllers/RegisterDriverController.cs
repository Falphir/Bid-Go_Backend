using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.DTOs;
using Bid_Go_Backend.Data.Repositories.Interfaces;
using BCrypt.Net;
using Microsoft.AspNetCore.Mvc;

namespace Bid_Go_Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RegisterDriverController : ControllerBase
    {
        private readonly IRegisterDriverRepository _driverRepository;

        public RegisterDriverController(IRegisterDriverRepository driverRepository)
        {
            _driverRepository = driverRepository;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDriverDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // check if email already exists
            var existingByEmail = await _driverRepository.GetByEmailAsync(dto.Email);
            if (existingByEmail != null)
                return Conflict(new { message = "Email is already registered." });

            // check if phone already exists
            var existingByPhone = await _driverRepository.GetByPhoneAsync(dto.PhoneNumber);
            if (existingByPhone != null)
                return Conflict(new { message = "Phone number is already registered." });

            // check if NIF already exists
            var existingByNIF = await _driverRepository.GetByNIFAsync(dto.NIF);
            if (existingByNIF != null)
                return Conflict(new { message = "Tax ID (NIF) is already registered." });

            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            var driver = new Driver
            {
                Name = dto.Name,
                DriverLicense = dto.DriverLicense,
                Insurance = dto.Insurance,
                Email = dto.Email,
                Password = hashedPassword,
                PhoneNumber = dto.PhoneNumber,
                NIF = dto.NIF
            };

            await _driverRepository.CreateAsync(driver);

            return Ok(new
            {
                message = "Driver account created successfully.",
                driver = new
                {
                    driver.Id,
                    driver.Name,
                    driver.DriverLicense,
                    driver.Email,
                    driver.Insurance,
                    driver.PhoneNumber,
                    driver.NIF
                }
            });
        }
    }
}