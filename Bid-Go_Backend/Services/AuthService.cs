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
        public virtual string GeneratePasswordResetToken()
        {
            return Guid.NewGuid().ToString("N"); 
        }
    }
}
