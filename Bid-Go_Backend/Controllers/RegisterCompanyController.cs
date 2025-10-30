using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.DTOs.CompanyDTOs;
using Bid_Go_Backend.Data.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;
using BCrypt.Net;

namespace Bid_Go_Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CompanyController : ControllerBase
    {
        private readonly IRegisterCompanyRepository _companyRepository;

        public CompanyController(IRegisterCompanyRepository companyRepository)
        {
            _companyRepository = companyRepository;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterCompanyDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // check if email already exists
            var existingByEmail = await _companyRepository.GetByEmailAsync(dto.Email);
            if (existingByEmail != null)
                return Conflict(new { message = "Email is already registered." });

            // check if phone already exists
            var existingByPhone = await _companyRepository.GetByPhoneAsync(dto.PhoneNumber);
            if (existingByPhone != null)
                return Conflict(new { message = "Phone number is already registered." });

            // check if NIF already exists
            var existingByNIF = await _companyRepository.GetByNIFAsync(dto.NIF);
            if (existingByNIF != null)
                return Conflict(new { message = "Tax ID (NIF) is already registered." });

            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            var company = new Company
            {
                Name = dto.Name,
                CompanyName = dto.CompanyName,
                Address = dto.Address,
                Email = dto.Email,
                Password = hashedPassword,
                PhoneNumber = dto.PhoneNumber,
                NIF = dto.NIF
            };

            await _companyRepository.CreateAsync(company);

            return Ok(new
            {
                message = "Company account created successfully.",
                company = new
                {
                    company.Id,
                    company.Name,
                    company.CompanyName,
                    company.Email,
                    company.Address,
                    company.PhoneNumber,
                    company.NIF
                }
            });
        }
    }
}
