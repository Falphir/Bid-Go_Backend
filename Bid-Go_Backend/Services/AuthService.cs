using Bid_Go_Backend.Data.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Bid_Go_Backend.Services
{
    public class AuthService
    {
        private readonly IConfiguration _config;

        public AuthService(IConfiguration config)
        {
            _config = config;
        }


        public string GeneratePasswordResetToken()
        {
            // Token seguro e aleatório
            return Convert.ToBase64String(Guid.NewGuid().ToByteArray());
        }
    }
}