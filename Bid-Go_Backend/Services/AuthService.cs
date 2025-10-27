using Bid_Go_Backend.Data.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using System;
namespace Bid_Go_Backend.Services
{

    public class AuthService
    {
        private readonly IMemoryCache _cache;

        public AuthService(IMemoryCache cache)
        {
            _cache = cache;
        }

        // Gera um token aleatório
        public string GeneratePasswordResetToken()
        {
            return Guid.NewGuid().ToString("N"); 
        }

        // Salva token na cache associado ao email do user
        public void SavePasswordResetToken(string token, string email)
        {
            _cache.Set(token, email, TimeSpan.FromHours(1));
        }

        // Recupera email a partir do token
        public string? GetEmailFromToken(string token)
        {
            _cache.TryGetValue(token, out string? email);
            return email;
        }
    }
}
