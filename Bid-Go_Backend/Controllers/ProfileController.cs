using Bid_Go_Backend.Data;
using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.DTOs;
using Bid_Go_Backend.Data.Models.Enums;
using Bid_Go_Backend.Repositories.Interface;
using Bid_Go_Backend.Repositories.ProfileRepo;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Text.Json;

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
                
                    Name = company.Name,
                    Email = company.Email,
                    PhoneNumber = company.PhoneNumber,
                    NIF = company.NIF,
                    CompanyName = company.CompanyName,
                    Address = company.Address
                });


            return BadRequest("Uknown user type");
        }

        // PUT /api/utilizadores/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProfile(int id, [FromBody] object dto)
        {
            var user = await _profileCrud.GetUserByIdAsync(id);
            if (user == null)
                return NotFound("User not found.");

            bool success = false;

            if (user is Driver)
            {
                var driverDto = dto as DriverProfileDTO;
                success = await _profileCrud.UpdateDriverAsync(id, driverDto!);
            }
            else if (user is Company)
            {
                var companyDto = dto as CompanyProfileDTO;
                success = await _profileCrud.UpdateCompanyAsync(id, companyDto!);
            }
            else
            {
                return BadRequest("Unknown user type.");
            }

            if (!success)
                return BadRequest("No valid fields provided for update.");

            return Ok("Profile updated successfully.");
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
