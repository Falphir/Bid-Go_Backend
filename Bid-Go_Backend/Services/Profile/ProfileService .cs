using Bid_Go_Backend.Data;
using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.DTOs;
using Bid_Go_Backend.Repositories.Interface;
using Bid_Go_Backend.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bid_Go_Backend.Services.Profile
{
    public class ProfileService : IProfileService
    {
        private readonly IProfileRepository _repo;
        private readonly ICloudflareR2Service _cloudflareR2;

        public ProfileService(IProfileRepository repo, ICloudflareR2Service cloudflareR2)
        {
            _repo = repo;
            _cloudflareR2 = cloudflareR2;
        }
        public async Task<User?> GetProfileAsync(int id)
        {
            var user = await _repo.GetUserByIdAsync(id);
            if (user == null) return null;
            if (!user.IsActive) throw new Exception("User is inactive.");
            return user;
        }

        public async Task<bool> UpdateDriverProfileAsync(int id, DriverProfileUpdateDTO dto)
        {
            var user = await _repo.GetUserByIdAsync(id);
            if (user is not Driver driver)
                throw new Exception("Driver not found.");
            if (!driver.IsActive) throw new Exception("Driver is inactive.");

            bool anyChange = false;

            if (!string.IsNullOrEmpty(dto.Name) && dto.Name != driver.Name)
            { driver.Name = dto.Name; anyChange = true; }

            if (!string.IsNullOrEmpty(dto.Email) && dto.Email != driver.Email)
            { driver.Email = dto.Email; anyChange = true; }

            if (dto.PhoneNumber.HasValue && dto.PhoneNumber.Value != driver.PhoneNumber)
            { driver.PhoneNumber = dto.PhoneNumber.Value; anyChange = true; }

            if (dto.NIF.HasValue && dto.NIF.Value != driver.NIF)
            { driver.NIF = dto.NIF.Value; anyChange = true; }

            // Upload nova carta de condução, se enviada
            if (dto.DriverLicense != null && dto.DriverLicense.Length > 0)
            {
                var licenseUrl = await _cloudflareR2.UploadImageAsync(dto.DriverLicense);
                driver.DriverLicense = licenseUrl;
                anyChange = true;
            }

            // Upload novo seguro, se enviado
            if (dto.Insurance != null && dto.Insurance.Length > 0)
            {
                var insuranceUrl = await _cloudflareR2.UploadImageAsync(dto.Insurance);
                driver.Insurance = insuranceUrl;
                anyChange = true;
            }

            if (!anyChange)
                throw new Exception("No valid fields provided to update.");

            await _repo.UpdateDriverAsync(driver);
            return true;
        }

        public async Task<bool> UpdateCompanyProfileAsync(int id, CompanyProfileDTO dto)
        {
            var user = await _repo.GetUserByIdAsync(id);
            if (user is not Company company)
                throw new Exception("Company not found.");
            if (!company.IsActive) throw new Exception("Company is inactive.");

            bool anyChange = false;

            if (!string.IsNullOrEmpty(dto.Name) && dto.Name != company.Name)
            { company.Name = dto.Name; anyChange = true; }

            if (!string.IsNullOrEmpty(dto.Email) && dto.Email != company.Email)
            { company.Email = dto.Email; anyChange = true; }

            if (dto.PhoneNumber.HasValue && dto.PhoneNumber.Value != company.PhoneNumber)
            { company.PhoneNumber = dto.PhoneNumber.Value; anyChange = true; }

            if (dto.NIF.HasValue && dto.NIF.Value != company.NIF)
            { company.NIF = dto.NIF.Value; anyChange = true; }

            if (!string.IsNullOrEmpty(dto.CompanyName) && dto.CompanyName != company.CompanyName)
            { company.CompanyName = dto.CompanyName; anyChange = true; }

            if (!string.IsNullOrEmpty(dto.Address) && dto.Address != company.Address)
            { company.Address = dto.Address; anyChange = true; }

            if (!anyChange)
                throw new Exception("No valid fields provided to update.");

            await _repo.UpdateCompanyAsync(company);
            return true;
        }


        public async Task<bool> ChangePasswordAsync(int id, string currentPassword, string newPassword)
        {
            var user = await _repo.GetUserByIdAsync(id);
            if (user == null) throw new Exception("User not found.");
            if (!user.IsActive) throw new Exception("User is inactive.");
            if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.Password))
                throw new Exception("Current password is incorrect.");

            user.Password = BCrypt.Net.BCrypt.HashPassword(newPassword);
            await _repo.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeactivateUserAsync(int id)
        {
            var user = await _repo.GetUserByIdAsync(id);
            if (user == null) throw new Exception("User not found.");
            if (!user.IsActive) throw new Exception("User is already inactive.");

            if (user is Driver)
            {
                bool hasActiveBids = await _repo.HasActiveBidsAsync(user.Id);
                if (hasActiveBids) throw new Exception("Driver cannot be deactivated with active bids.");
            }
            else if (user is Company)
            {
                bool hasActiveRequests = await _repo.HasActiveTransportRequestsAsync(user.Id);
                if (hasActiveRequests) throw new Exception("Company cannot be deactivated with active transport requests.");
            }

            user.IsActive = false;
            await _repo.SaveChangesAsync();
            return true;
        }

    }

}