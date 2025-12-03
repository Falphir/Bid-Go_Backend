using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.DTOs;
using Bid_Go_Backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bid_Go_Backend.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/profile")]
    public class ProfileController : ControllerBase
    {
        private readonly IProfileService _service;

        public ProfileController(IProfileService service)
        {
            _service = service;
        }

        /// <summary>
        /// Get the profile for the authenticated user. Caller must match the requested user id.
        /// </summary>
        /// <param name="id">User identifier to fetch profile for.</param>
        /// <returns>Driver or Company profile DTO.</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetProfile(int id)
        {
            var userIdClaim = User.FindFirst("userId")?.Value;
            if (userIdClaim == null || int.Parse(userIdClaim) != id)
                return Forbid();


            try
            {
                var user = await _service.GetProfileAsync(id);

                if (user is Driver driver)
                    return Ok(new DriverProfileDTO
                    {
                        Name = driver.Name,
                        ProfileImage = driver.ProfileImage,
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
                        ProfileImage = company.ProfileImage,
                        Email = company.Email,
                        PhoneNumber = company.PhoneNumber,
                        NIF = company.NIF,
                        CompanyName = company.CompanyName,
                        Address = company.Address
                    });

                return BadRequest("Unknown user type.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Update driver profile fields. User must be the driver and own the profile.
        /// </summary>
        [Authorize(Policy = "DriverOnly")]
        [HttpPut("updateDriver/{id}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UpdateDriverProfile(int id, [FromForm] DriverProfileUpdateDTO dto)
        {
            var userIdClaim = User.FindFirst("userId")?.Value;

            if (int.Parse(userIdClaim) != id)
                return Forbid();

            try
            {
                var success = await _service.UpdateDriverProfileAsync(id, dto);
                if (!success)
                    return BadRequest("No valid fields provided.");

                return Ok("Driver profile updated successfully.");
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Update company profile fields. User must be the company and own the profile.
        /// </summary>
        [Authorize(Policy = "CompanyOnly")]
        [HttpPut("updateCompany/{id}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UpdateCompanyProfile(int id, [FromForm] CompanyProfileUpdateDTO dto)
        {
            var userIdClaim = User.FindFirst("userId")?.Value;

            if (int.Parse(userIdClaim) != id)
                return Forbid();
            try
            {
                var success = await _service.UpdateCompanyProfileAsync(id, dto);
                if (!success)
                    return BadRequest("No valid fields provided.");

                return Ok("Company profile updated successfully.");
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Change user password. Caller must be the same user.
        /// </summary>
        [HttpPut("{id}/changePassword")]
        public async Task<IActionResult> ChangePassword(int id, [FromBody] ChangePasswordDTO dto)
        {

            var userIdClaim = User.FindFirst("userId")?.Value;
            if (userIdClaim == null || int.Parse(userIdClaim) != id)
                return Forbid();

            try
            {
                await _service.ChangePasswordAsync(id, dto.CurrentPassword, dto.NewPassword);
                return Ok("Password changed successfully.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Deactivate a user account. Caller must be the same user.
        /// </summary>
        [HttpPut("{id}/deactivateAccount")]
        public async Task<IActionResult> DeactivateUser(int id)
        {

            var userIdClaim = User.FindFirst("userId")?.Value;
            if (userIdClaim == null || int.Parse(userIdClaim) != id)
                return Forbid();

            try
            {
                await _service.DeactivateUserAsync(id);
                return Ok("User deactivated successfully.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }

}
