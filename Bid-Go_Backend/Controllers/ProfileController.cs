using Bid_Go_Backend.Data;
using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.DTOs;
using Bid_Go_Backend.Data.Models.Enums;
using Bid_Go_Backend.Repositories.Interface;
using Bid_Go_Backend.Repositories.ProfileRepo;
using Bid_Go_Backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

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

                return BadRequest("Unknown user type.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [Authorize(Policy = "DriverOnly")]
        [HttpPut("driver/{id}")]
        public async Task<IActionResult> UpdateDriverProfile(int id, [FromForm] DriverProfileUpdateDTO dto)
        {
            var userIdClaim = User.FindFirst("userId")?.Value;

            if (userIdClaim == null)
                return Unauthorized(new { message = "Token inválido ou utilizador não autenticado." });

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

        [Authorize(Policy = "CompanyOnly")]
        [HttpPut("company/{id}")]
        public async Task<IActionResult> UpdateCompanyProfile(int id, [FromBody] CompanyProfileDTO dto)
        {

            var userIdClaim = User.FindFirst("userId")?.Value;

            if (userIdClaim == null)
                return Unauthorized(new { message = "Token inválido ou utilizador não autenticado." });

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

        [HttpPut("{id}/ChangePassword")]
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

        [HttpPut("{id}/deactivate")]
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
