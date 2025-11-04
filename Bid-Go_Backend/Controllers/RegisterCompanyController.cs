using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.DTOs.CompanyDTOs;
using Bid_Go_Backend.Data.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Bid_Go_Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CompanyController : ControllerBase
    {
        private readonly IRegisterCompanyService _companyService;
        

        public CompanyController(IRegisterCompanyService companyService)
        {
            _companyService = companyService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterCompanyDTO dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var (success, error, company) = await _companyService.RegisterAsync(dto);

            if (!success)
            {
                return error switch
                {
                    "EMAIL_EXISTS" => Conflict(new { message = "Email is already registered." }),
                    "PHONE_EXISTS" => Conflict(new { message = "Phone number is already registered." }),
                    "NIF_EXISTS" => Conflict(new { message = "Tax ID (NIF) is already registered." }),
                    _ => StatusCode(500, new { message = "Unexpected error." })
                };
            }

            return Ok(new
            {
                message = "Company account created successfully.",
                company = new
                {
                    company!.Id,
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
