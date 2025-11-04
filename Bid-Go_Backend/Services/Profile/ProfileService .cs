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

        public ProfileService(IProfileRepository repo)
        {
            _repo = repo;
        }

        public async Task<User?> GetProfileAsync(int id)
        {
            var user = await _repo.GetUserByIdAsync(id);
            if (user == null) return null;
            if (!user.IsActive) throw new Exception("User is inactive.");
            return user;
        }

        public async Task<bool> UpdateProfileAsync(int id, object dto)
        {
            var user = await _repo.GetUserByIdAsync(id);
            if (user == null) throw new Exception("User not found.");
            if (!user.IsActive) throw new Exception("User is inactive.");

            bool updated = false;

            if (user is Driver driver)
            {
                var driverDto = dto as DriverProfileDTO ?? throw new Exception("Invalid driver data.");

                bool anyChange = false;

                if (!string.IsNullOrEmpty(driverDto.Name) && driverDto.Name != driver.Name)
                { driver.Name = driverDto.Name; anyChange = true; }

                if (!string.IsNullOrEmpty(driverDto.Email) && driverDto.Email != driver.Email)
                { driver.Email = driverDto.Email; anyChange = true; }

                if (driverDto.PhoneNumber.HasValue && driverDto.PhoneNumber.Value != driver.PhoneNumber)
                { driver.PhoneNumber = driverDto.PhoneNumber.Value; anyChange = true; }

                if (driverDto.NIF.HasValue && driverDto.NIF.Value != driver.NIF)
                { driver.NIF = driverDto.NIF.Value; anyChange = true; }

                if (!string.IsNullOrEmpty(driverDto.DriverLicense) && driverDto.DriverLicense != driver.DriverLicense)
                { driver.DriverLicense = driverDto.DriverLicense; anyChange = true; }

                if (!string.IsNullOrEmpty(driverDto.Insurance) && driverDto.Insurance != driver.Insurance)
                { driver.Insurance = driverDto.Insurance; anyChange = true; }

                if (!anyChange)
                    throw new Exception("No valid fields provided to update.");

                await _repo.UpdateDriverAsync(driver);
                updated = true;
            }
            else if (user is Company company)
            {
                var companyDto = dto as CompanyProfileDTO ?? throw new Exception("Invalid company data.");

                bool anyChange = false;

                if (!string.IsNullOrEmpty(companyDto.Name) && companyDto.Name != company.Name)
                { company.Name = companyDto.Name; anyChange = true; }

                if (!string.IsNullOrEmpty(companyDto.Email) && companyDto.Email != company.Email)
                { company.Email = companyDto.Email; anyChange = true; }

                if (companyDto.PhoneNumber.HasValue && companyDto.PhoneNumber.Value != company.PhoneNumber)
                { company.PhoneNumber = companyDto.PhoneNumber.Value; anyChange = true; }

                if (companyDto.NIF.HasValue && companyDto.NIF.Value != company.NIF)
                { company.NIF = companyDto.NIF.Value; anyChange = true; }

                if (!string.IsNullOrEmpty(companyDto.CompanyName) && companyDto.CompanyName != company.CompanyName)
                { company.CompanyName = companyDto.CompanyName; anyChange = true; }

                if (!string.IsNullOrEmpty(companyDto.Address) && companyDto.Address != company.Address)
                { company.Address = companyDto.Address; anyChange = true; }

                if (!anyChange)
                    throw new Exception("No valid fields provided to update.");

                await _repo.UpdateCompanyAsync(company);
                updated = true;
            }
            else
            {
                throw new Exception("Unknown user type.");
            }

            return updated;
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