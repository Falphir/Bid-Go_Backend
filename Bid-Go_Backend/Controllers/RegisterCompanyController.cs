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

            // Verificar se já existe
            var existingByEmail = await _companyRepository.GetByEmailAsync(dto.Email);
            if (existingByEmail != null)
                return Conflict(new { message = "O email já está registado." });

            var existingByPhone = await _companyRepository.GetByPhoneAsync(dto.PhoneNumber);
            if (existingByPhone != null)
                return Conflict(new { message = "O número de telefone já está registado." });

            var existingByNIF = await _companyRepository.GetByNIFAsync(dto.NIF);
            if (existingByNIF != null)
                return Conflict(new { message = "O NIF já está registado." });

      
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
                message = "Conta criada com sucesso.",
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
