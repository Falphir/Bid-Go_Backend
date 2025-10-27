using Bid_Go_Backend.Data.Models.DTOs;
using Bid_Go_Backend.Data.Repositories.Interfaces;
using Bid_Go_Backend.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

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

        [HttpPost("recover-password")]
        public async Task<IActionResult> RecoverPassword([FromBody] RecoverPasswordRequestDTO request)
        {
            var user = await _userRepository.GetByEmailAsync(request.Email);
            if (user == null)
                return NotFound(new { message = "Utilizador não encontrado com esse email." });

            user.PasswordResetToken = _authService.GeneratePasswordResetToken();
            user.ResetTokenExpiryTime = DateTime.UtcNow.AddHours(1);
            await _userRepository.UpdateAsync(user);

            return Ok(new { message = "Instruções de recuperação enviadas por email." });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDTO request)
        {
            var user = await _userRepository.GetByResetTokenAsync(request.Token);
            if (user == null || user.ResetTokenExpiryTime < DateTime.UtcNow)
                return BadRequest(new { message = "Token inválido ou expirado." });

            // Futuramente acrescentar o hash NÂO ESQUECER
            user.Password = request.NewPassword;
            user.PasswordResetToken = null;
            user.ResetTokenExpiryTime = null;
            await _userRepository.UpdateAsync(user);

            return Ok(new { message = "Password alterada com sucesso." });
        }
    }
}
