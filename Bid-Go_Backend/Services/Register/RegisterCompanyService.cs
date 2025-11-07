using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.DTOs.CompanyDTOs;
using Bid_Go_Backend.Repositories.Interfaces;
using Bid_Go_Backend.Services.Interfaces;

namespace Bid_Go_Backend.Services.Register
{
    /// <summary>
    /// Service for company registration business rules.
    /// </summary>
    public class RegisterCompanyService : IRegisterCompanyService
    {
        private readonly IRegisterCompanyRepository _repo;

        public RegisterCompanyService(IRegisterCompanyRepository repo)
        {
            _repo = repo;
        }

        /// <summary>
        /// Register a company after validating uniqueness constraints.
        /// </summary>
        /// <returns>Tuple with success flag, error code when any, and created company.</returns>
        public async Task<(bool Success, string? Error, Company? Company)> RegisterAsync(RegisterCompanyDTO dto)
        {
            if (await _repo.GetByEmailAsync(dto.Email) is not null)
                return (false, "EMAIL_EXISTS", null);

            if (await _repo.GetByPhoneAsync(dto.PhoneNumber) is not null)
                return (false, "PHONE_EXISTS", null);

            if (await _repo.GetByNIFAsync(dto.NIF) is not null)
                return (false, "NIF_EXISTS", null);

            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            var company = new Company
            {
                Name = dto.Name,
                CompanyName = dto.CompanyName,
                Address = dto.Address,
                Email = dto.Email,
                Password = hashedPassword,
                PhoneNumber = dto.PhoneNumber,
                NIF = dto.NIF
            };

            var created = await _repo.CreateAsync(company);
            return (true, null, created);
        }
    }
}
