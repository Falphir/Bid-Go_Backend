using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.DTOs;
using Bid_Go_Backend.Repositories.Interfaces;
using Bid_Go_Backend.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bid_Go_Backend.Services.Register
{
    /// <summary>
    /// Service for driver registration including document uploads.
    /// </summary>
    public class RegisterDriverService : IRegisterDriverService
    {
        private readonly IRegisterDriverRepository _repo;
        private readonly ICloudflareR2Service _cloudflareR2Service;
        public RegisterDriverService(IRegisterDriverRepository repo, ICloudflareR2Service cloudflareR2Service)
        {
            _repo = repo;
            _cloudflareR2Service = cloudflareR2Service;
        }

        /// <summary>
        /// Register a driver after validating uniqueness and uploading required documents.
        /// </summary>
        public async Task<(bool Success, string? Error, Driver? Driver)> RegisterAsync(RegisterDriverDTO dto)
        {
            if (await _repo.GetByEmailAsync(dto.Email) is not null)
                return (false, "EMAIL_EXISTS", null);

            if (await _repo.GetByPhoneAsync(dto.PhoneNumber) is not null)
                return (false, "PHONE_EXISTS", null);

            if (await _repo.GetByNIFAsync(dto.NIF) is not null)
                return (false, "NIF_EXISTS", null);

            var licenseUrl = await _cloudflareR2Service.UploadImageAsync(dto.DriverLicense);
            var insuranceUrl = await _cloudflareR2Service.UploadImageAsync(dto.Insurance);

            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            var driver = new Driver
            {
                Name = dto.Name,
                DriverLicense = licenseUrl,
                Insurance = insuranceUrl,
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
