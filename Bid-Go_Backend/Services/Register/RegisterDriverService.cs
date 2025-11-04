using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.DTOs;
using Bid_Go_Backend.Data.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bid_Go_Backend.Services.Register
{
    public class RegisterDriverService : IRegisterDriverService
    {
        private readonly IRegisterDriverRepository _repo;

        public RegisterDriverService(IRegisterDriverRepository repo)
        {
            _repo = repo;
        }

        public async Task<(bool Success, string? Error, Driver? Driver)> RegisterAsync(RegisterDriverDTO dto)
        {
            if (await _repo.GetByEmailAsync(dto.Email) is not null)
                return (false, "EMAIL_EXISTS", null);

            if (await _repo.GetByPhoneAsync(dto.PhoneNumber) is not null)
                return (false, "PHONE_EXISTS", null);

            if (await _repo.GetByNIFAsync(dto.NIF) is not null)
                return (false, "NIF_EXISTS", null);

            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            var driver = new Driver
            {
                Name = dto.Name,
                DriverLicense = dto.DriverLicense,
                Insurance = dto.Insurance,
                Email = dto.Email,
                Password = hashedPassword,
                PhoneNumber = dto.PhoneNumber,
                NIF = dto.NIF
            };

            var created = await _repo.CreateAsync(driver);
            return (true, null, created);
        }
    }
}
