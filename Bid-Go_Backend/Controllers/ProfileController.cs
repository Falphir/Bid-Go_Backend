using Bid_Go_Backend.Data;
using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.DTOs;
using Bid_Go_Backend.Data.Models.Enums;
using Bid_Go_Backend.Repositories.Interface;
using Bid_Go_Backend.Repositories.ProfileRepo;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace Bid_Go_Backend.Controllers
{
    [ApiController]
    [Route("api/profile")] // Rota raiz
    public class ProfileController : ControllerBase
    {

        private readonly IProfileCrud _profileCrud;
        private readonly BidGoDbContext _ctx;
        public ProfileController(IProfileCrud profileCrud, BidGoDbContext ctx)
        {
            _profileCrud = profileCrud;
            _ctx = ctx;
        }

        // GET /utilizadores/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetProfile(int id)
        {
            var user = await _profileCrud.GetUserByIdAsync(id);

            if (user == null)
                return NotFound("User not found");

            if (user is Driver driver)
                return Ok(new DriverProfileDTO
                {
                    Id = driver.Id,
                    Name = driver.Name,
                    Email = driver.Email,
                    PhoneNumber = driver.PhoneNumber,
                    NIF = driver.NIF,
                    DriverLicense = driver.DriverLicense,
                    Insurance = driver.Insurance
                });

            if (user is Company company)
                return Ok(new CompanyProfileDTO
                {
                    Id = company.Id,
                    Name = company.Name,
                    Email = company.Email,
                    PhoneNumber = company.PhoneNumber,
                    NIF = company.NIF,
                    CompanyName = company.CompanyName,
                    Address = company.Address
                });


            return BadRequest("Uknown user type");
        }

        // PUT /api/utilizadores/motorista/{id}
        [HttpPut("motorista/{id}")]
        public async Task<IActionResult> UpdateDriverProfile(int id, [FromBody] DriverProfileDTO dto)
        {
            var success = await _profileCrud.UpdateDriverAsync(id, dto);
            if (!success)
                return NotFound("Driver not found.");

            return Ok("Driver profile updated successfully.");
        }

        // PUT /api/utilizadores/empresa/{id}
        [HttpPut("empresa/{id}")]
        public async Task<IActionResult> UpdateCompanyProfile(int id, [FromBody] CompanyProfileDTO dto)
        {
            var success = await _profileCrud.UpdateCompanyAsync(id, dto);
            if (!success)
                return NotFound("Company not found.");

            return Ok("Company profile updated successfully.");
        }

   

        
        
        // DELETE /utilizadores/{id}

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            await _profileCrud.DeleteUserAsync(id);
            return Ok("User deleted successfully.");
        }
    }
}
