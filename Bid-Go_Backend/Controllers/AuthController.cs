using Bid_Go_Backend.Data.Models.DTOs;
using Bid_Go_Backend.Data.Repositories.Interfaces;
using Bid_Go_Backend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

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
}
