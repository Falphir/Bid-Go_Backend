using Bid_Go_Backend.Data.Models.DTOs.LoginDTOs;
using Bid_Go_Backend.Data.Repositories.Interfaces;
using Bid_Go_Backend.DTOs;
using Bid_Go_Backend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
﻿using Bid_Go_Backend.Data.Models.DTOs;
using Bid_Go_Backend.Data.Repositories.Interfaces;
using Bid_Go_Backend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace Bid_Go_Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly AuthService _authService;
    private readonly EmailService _emailService;
    private readonly IMemoryCache _cache;

          public AuthController(
        IUserRepository userRepository,
        AuthService authService,
        EmailService emailService,
        IMemoryCache cache)
    {
        _userRepository = userRepository;
        _authService = authService;
        _emailService = emailService;
        _cache = cache;
    }
    

[HttpPost("recover-password")]
    public async Task<IActionResult> RecoverPassword([FromBody] RecoverPasswordRequestDTO request)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email);
        if (user == null)
            return NotFound(new { message = "Utilizador não encontrado." });

        // Gera token temporário
        var token = _authService.GeneratePasswordResetToken();

        // Guarda na cache por 1 hora
        _cache.Set(token, user.Email, TimeSpan.FromHours(1));

        // token de reset
        await _emailService.SendEmailAsync(
            user.Email,
            "Recuperação de password",
            $"Tem aqui o seu token '{token}' para redefinir a sua password."
        );

        return Ok(new { message = "Instruções enviadas por email." });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDTO request)
    {
        if (!_cache.TryGetValue(request.Token, out string email))
            return BadRequest(new { message = "Token inválido ou expirado." });

        var user = await _userRepository.GetByEmailAsync(email);
        if (user == null)
            return NotFound(new { message = "Utilizador não encontrado." });

        // Atualiza a password com hash
        user.Password = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        await _userRepository.UpdateAsync(user);

        // Remove token da cache
        _cache.Remove(request.Token);

        return Ok(new { message = "Password alterada com sucesso." });
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

            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.Password))
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