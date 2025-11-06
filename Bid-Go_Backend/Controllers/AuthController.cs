using Bid_Go_Backend.Data.Models.DTOs;
using Bid_Go_Backend.Data.Models.DTOs.LoginDTOs;
using Bid_Go_Backend.Services.Interfaces;
using Bid_Go_Backend.DTOs;
using Bid_Go_Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace Bid_Go_Backend.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("recover-password")]
        public async Task<IActionResult> RecoverPassword([FromBody] RecoverPasswordRequestDTO request)
        {
            var result = await _authService.RecoverPasswordAsync(request.Email);
            return StatusCode(result.StatusCode, new { message = result.Message });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDTO request)
        {
            var result = await _authService.ResetPasswordAsync(request.Token, request.NewPassword);
            return StatusCode(result.StatusCode, new { message = result.Message });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            var result = await _authService.LoginAsync(request.Email, request.Password);

            if (!result.Success)
                return Unauthorized(new { message = result.Message });

            return Ok(new
            {
                token = result.Token,
                expiration = result.Expiration
            });
        }

        [HttpGet("me")]
        [Authorize]
        public IActionResult Me()
        {
            var claims = User.Claims.Select(c => new
            {
                Type = c.Type,
                Value = c.Value
            });

            return Ok(new
            {
                message = "Token claims",
                claims
            });
        }

        [HttpGet("company-endpoint")]
        [Authorize(Policy = "CompanyOnly")]
        public IActionResult CompanyEndpoint() =>
            Ok(new { message = "Apenas Companies conseguem ver isto!" });

        [HttpGet("driver-endpoint")]
        [Authorize(Policy = "DriverOnly")]
        public IActionResult DriverEndpoint() =>
            Ok(new { message = "Apenas Drivers conseguem ver isto!" });
    }
}
