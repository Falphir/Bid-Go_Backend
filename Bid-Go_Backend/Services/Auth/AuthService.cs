using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Repositories.Interfaces;
using Bid_Go_Backend.Services.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Bid_Go_Backend.Services.Auth
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _config;
        private readonly IMemoryCache _cache;

        public AuthService(
            IUserRepository userRepository,
            IEmailService emailService,
            IConfiguration config,
            IMemoryCache cache)
        {
            _userRepository = userRepository;
            _emailService = emailService;
            _config = config;
            _cache = cache;
        }


        public async Task<(bool Success, string Message, string Token, DateTime? Expiration)> LoginAsync(string email, string password)
        {
            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null)
                return (false, "Email inválido.", null, null);

            if (!BCrypt.Net.BCrypt.Verify(password, user.Password))
                return (false, "Password incorreta.", null, null);

            var token = GenerateJwtToken(user);
            var expiration = DateTime.UtcNow.AddMinutes(double.Parse(_config["Jwt:ExpireMinutes"]!));

            return (true, "Login bem-sucedido", token, expiration);
        }


        public async Task<(int StatusCode, string Message)> RecoverPasswordAsync(string email)
        {
            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null)
                return (404, "Utilizador não encontrado.");

            var token = GeneratePasswordResetToken();
            _cache.Set(token, user.Email, TimeSpan.FromHours(1));

            await _emailService.SendEmailAsync(
                user.Email,
                "Recuperação de password",
                $"Tem aqui o seu token '{token}' para redefinir a sua password."
            );

            return (200, "Instruções enviadas por email.");
        }

        public async Task<(int StatusCode, string Message)> ResetPasswordAsync(string token, string newPassword)
        {
            if (!_cache.TryGetValue(token, out string email))
                return (400, "Token inválido ou expirado.");

            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null)
                return (404, "Utilizador não encontrado.");

            user.Password = BCrypt.Net.BCrypt.HashPassword(newPassword);
            await _userRepository.UpdateAsync(user);
            _cache.Remove(token);

            return (200, "Password alterada com sucesso.");
        }

        private string GeneratePasswordResetToken() =>
            Guid.NewGuid().ToString("N");

        public string GenerateJwtToken(User user)
        {
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Email),
                new Claim("userId", user.Id.ToString()),
                new Claim("userType", user.GetType().Name)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(double.Parse(_config["Jwt:ExpireMinutes"]!)),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

    }
}
