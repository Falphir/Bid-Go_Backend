using Bid_Go_Backend.Data;
using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.DTOs;
using Bid_Go_Backend.Data.Models.Enums;
using Bid_Go_Backend.Repositories.Interface;
using Bid_Go_Backend.Repositories.ProfileRepo;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
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

            if (!user.IsActive)
                return Conflict("User is inactive and cannot be updated.");

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
        public async Task<IActionResult> UpdateProfile(int id, [FromBody] JsonElement dto)
        {

            var user = await _profileCrud.GetUserByIdAsync(id);
            if (user == null)
                return NotFound("User not found.");


            if (!user.IsActive) 
                return BadRequest("This account is deactivated and cannot be updated.");


            bool success = false;

          
            if (user is Driver)
            {
                var driverDto = JsonConvert.DeserializeObject<DriverProfileDTO>(dto.ToString());
                var context = new ValidationContext(driverDto!);
                var results = new List<ValidationResult>();
                if (!Validator.TryValidateObject(driverDto!, context, results, true))
                    return BadRequest(results.Select(r => r.ErrorMessage));
                success = await _profileCrud.UpdateDriverAsync(id, driverDto!);
            }
            else if (user is Company)
            {
                var companyDto = JsonConvert.DeserializeObject<CompanyProfileDTO>(dto.ToString());
                var context = new ValidationContext(companyDto!);
                var results = new List<ValidationResult>();
                if (!Validator.TryValidateObject(companyDto!, context, results, true))
                    return BadRequest(results.Select(r => r.ErrorMessage));
                success = await _profileCrud.UpdateCompanyAsync(id, companyDto!);
            }
            else
            {
                return BadRequest("Unknown user type.");
            }

            if (!success)
                return BadRequest("No valid fields provided or user not found.");

            return Ok("Profile updated successfully.");
        }


        // PUT /utilizadores/{id}
        [HttpPut("{id}/ChangePassword")]
        public async Task<IActionResult> ChangePassword(int id, [FromBody] ChangePasswordDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                await _profileCrud.ChangePasswordAsync(id, dto.CurrentPassword, dto.NewPassword);
                return Ok("Password changed successfully.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


        // DELETE /utilizadores/{id}
        [HttpPut("{id}/desativar")]
        public async Task<IActionResult> DeactivateUser(int id)
        {
            try
            {
                await _profileCrud.DeactivateUserAsync(id);
                return Ok("User deactivated successfully.");
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }


        }
    }
}
