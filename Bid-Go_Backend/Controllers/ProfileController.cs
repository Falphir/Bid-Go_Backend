using Bid_Go_Backend.Data;
using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.DTOs;
using Bid_Go_Backend.Data.Models.Enums;
using Bid_Go_Backend.Repositories.Interface;
using Bid_Go_Backend.Repositories.ProfileRepo;
using Bid_Go_Backend.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Bid_Go_Backend.Controllers
{
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

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProfile(int id, [FromBody] JsonElement dto)
        {
            try
            {
                object profileDto;
                var user = await _service.GetProfileAsync(id);
                if (user is Driver)
                    profileDto = JsonConvert.DeserializeObject<DriverProfileDTO>(dto.ToString())!;
                else
                    profileDto = JsonConvert.DeserializeObject<CompanyProfileDTO>(dto.ToString())!;

                var success = await _service.UpdateProfileAsync(id, profileDto);
                if (!success) return BadRequest("No valid fields provided.");

                return Ok("Profile updated successfully.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("{id}/ChangePassword")]
        public async Task<IActionResult> ChangePassword(int id, [FromBody] ChangePasswordDTO dto)
        {
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
