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

        /// <summary>
        /// Initiates a password recovery flow by sending a recovery email to the provided address.
        /// </summary>
        /// <param name="request">Contains the email to recover the password for.</param>
        /// <returns>HTTP result indicating success or failure of the recovery initiation.</returns>
        [HttpPost("recover-password")]
        public async Task<IActionResult> RecoverPassword([FromBody] RecoverPasswordRequestDTO request)
        {
            var result = await _authService.RecoverPasswordAsync(request.Email);
            return StatusCode(result.StatusCode, new { message = result.Message });
        }

        /// <summary>
        /// Resets a user's password using a valid recovery token.
        /// </summary>
        /// <param name="request">Token and new password payload.</param>
        /// <returns>HTTP result with operation message.</returns>
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDTO request)
        {
            var result = await _authService.ResetPasswordAsync(request.Token, request.NewPassword);
            return StatusCode(result.StatusCode, new { message = result.Message });
        }

        /// <summary>
        /// Authenticates a user and returns a JWT token when credentials are valid.
        /// </summary>
        /// <param name="request">Login credentials (email and password).</param>
        /// <returns>JWT token and expiration or 401 Unauthorized on failure.</returns>
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

        /// <summary>
        /// Returns the claims present in the authenticated user's token.
        /// </summary>
        /// <remarks>
        /// Useful for debugging tokens from the client side; no business logic here.
        /// </remarks>
        /// <returns>List of claim type/value pairs.</returns>
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

        /// <summary>
        /// Sample endpoint restricted to company users. Kept for authorization testing.
        /// </summary>
        [HttpGet("company-endpoint")]
        [Authorize(Policy = "CompanyOnly")]
        public IActionResult CompanyEndpoint() =>
            Ok(new { message = "Only companies can access this endpoint." });

        /// <summary>
        /// Sample endpoint restricted to driver users. Kept for authorization testing.
        /// </summary>
        [HttpGet("driver-endpoint")]
        [Authorize(Policy = "DriverOnly")]
        public IActionResult DriverEndpoint() =>
            Ok(new { message = "Only drivers can access this endpoint." });
    }
}
