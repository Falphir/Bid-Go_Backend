using Bid_Go_Backend.Data.Models.DTOs;
using Bid_Go_Backend.Data.Repositories.Interfaces;
using Bid_Go_Backend.Services;
using Microsoft.AspNetCore.Mvc;

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

        var token = _authService.GeneratePasswordResetToken();
        _authService.SavePasswordResetToken(token, user.Email);

        var resetLink = $"https://siteteste.com/reset-password?token={token}";
        Console.WriteLine($"[TESTE] Link de recover: {resetLink}");

        return Ok(new { message = "Link de recuperação gerado (ver console para testes)." });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDTO request)
    {
        var email = _authService.GetEmailFromToken(request.Token);
        if (email == null)
            return BadRequest(new { message = "Token inválido ou expirado." });

        var user = await _userRepository.GetByEmailAsync(email);
        if (user == null)
            return BadRequest(new { message = "Utilizador não encontrado." });

        // Atualiza a password com hash
        user.Password = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        await _userRepository.UpdateAsync(user);

        return Ok(new { message = "Password alterada com sucesso." });
    }
}
