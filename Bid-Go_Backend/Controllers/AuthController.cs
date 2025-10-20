using Bid_Go_Backend.Data.Models.DTOs.LoginDTOs;
using Bid_Go_Backend.Data.Repositories.Interfaces;
using Bid_Go_Backend.DTOs;
using Bid_Go_Backend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace Bid_Go_Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly AuthService _authService;

        public AuthController(IUserRepository userRepository, AuthService authService)
        {
            _userRepository = userRepository;
            _authService = authService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            var user = await _userRepository.GetByEmailAsync(request.Email);
            if (user == null)
                return Unauthorized(new { message = "Email inválido." });


            Console.WriteLine($"Email do request: '{request.Email}'");
            Console.WriteLine($"Password do request: '{request.Password}'");
            Console.WriteLine($"Email do DB: '{user.Email}'");
            Console.WriteLine($"Password do DB: '{user.Password}'");

            if (user.Password != request.Password)
                return Unauthorized(new { message = "Password incorreta." });


            var token = _authService.GenerateJwtToken(user);
        

            return Ok(new LoginResponseDto
            {

                Token = token,
                Expiration = DateTime.UtcNow.AddMinutes(60)
            });
        }
        // Endpoint protegido com JWT
        [HttpGet("me")]
        [Authorize]
        public IActionResult Me()
        {
            var userEmail = User.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
            var userId = User.Claims.FirstOrDefault(c => c.Type == "userId")?.Value;
            var userType = User.Claims.FirstOrDefault(c => c.Type == "userType")?.Value;

            return Ok(new
            {
                message = "Endpoint protegido",
                email = userEmail,
            });
        }

        // Apenas Companys podem aceder
        [HttpGet("company-endpoint")]
        [Authorize(Policy = "CompanyOnly")]
        public IActionResult CompanyEndpoint()
        {
            return Ok(new { message = "Apenas Companies conseguem ver isto!" });
        }

        // Apenas Drivers podem aceder
        [HttpGet("driver-endpoint")]
        [Authorize(Policy = "DriverOnly")]
        public IActionResult DriverEndpoint()
        {
            return Ok(new { message = "Apenas Drivers conseguem ver isto!" });
        }

    }
}
