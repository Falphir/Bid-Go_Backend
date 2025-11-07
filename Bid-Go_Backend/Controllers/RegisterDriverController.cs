using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.DTOs;
using BCrypt.Net;
using Microsoft.AspNetCore.Mvc;
using Bid_Go_Backend.Services.Interfaces;

namespace Bid_Go_Backend.Controllers
{
    [ApiController]
    [Route("api/register")]
    public class RegisterDriverController : ControllerBase
    {
        private readonly IRegisterDriverService _driverService;

        public RegisterDriverController(IRegisterDriverService driverService)
        {
            _driverService = driverService;
        }

        /// <summary>
        /// Register a new driver account.
        /// </summary>
        /// <remarks>
        /// Controller performs request validation and maps service error codes to HTTP responses.
        /// Password hashing and storage are handled in the service/repository layers; the controller remains thin.
        /// </remarks>
        /// <param name="dto">Driver registration payload submitted as multipart/form-data (includes files).</param>
        /// <returns>Created driver summary or conflict/error responses.</returns>
        [HttpPost("driver")]
        public async Task<IActionResult> Register([FromForm] RegisterDriverDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var (success, error, driver) = await _driverService.RegisterAsync(dto);

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
                message = "Driver account created successfully.",
                driver = new
                {
                    driver!.Id,
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