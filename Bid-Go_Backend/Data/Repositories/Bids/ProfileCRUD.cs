using Bid_Go_Backend.Data;
using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Repositories.Interface;
using Microsoft.EntityFrameworkCore;
using Bid_Go_Backend.Data.Models.DTOs;
using System.Security.Cryptography.X509Certificates;

namespace Bid_Go_Backend.Repositories.ProfileRepo
{
    public class ProfileCRUD : IProfileCrud
    {
        private readonly BidGoDbContext _ctx;

        public ProfileCRUD(BidGoDbContext ctx)
        {
            _ctx = ctx;
        }

        public async Task<User?> GetUserByIdAsync(int id)
        {
            return await _ctx.Users.FirstOrDefaultAsync(u => u.Id == id);
        }


        public async Task<bool> UpdateDriverAsync(int id, DriverProfileDTO dto)
        {
            var driver = await _ctx.Drivers.FindAsync(id);
            if (driver == null)
                return false;

            if (!string.IsNullOrEmpty(dto.Name))
                driver.Name = dto.Name;

            if (!string.IsNullOrEmpty(dto.Email))
                driver.Email = dto.Email;

            if (dto.PhoneNumber.HasValue)
                driver.PhoneNumber = dto.PhoneNumber.Value;

            if (dto.NIF.HasValue)
                driver.NIF = dto.NIF.Value;

            if (!string.IsNullOrEmpty(dto.DriverLicense))
                driver.DriverLicense = dto.DriverLicense;

            if (!string.IsNullOrEmpty(dto.Insurance))
                driver.Insurance = dto.Insurance;

        
            await _ctx.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateCompanyAsync(int id, CompanyProfileDTO dto)
        {
            var company = await _ctx.Companies.FindAsync(id);
            if (company == null)
                return false;

            if(!string.IsNullOrEmpty(dto.Name))
                company.Name = dto.Name;

            if (!string.IsNullOrEmpty(dto.Email))
                company.Email = dto.Email;

            if (dto.PhoneNumber.HasValue)
                company.PhoneNumber = dto.PhoneNumber.Value;

            if (dto.NIF.HasValue)
                company.NIF = dto.NIF.Value;

            if (!string.IsNullOrEmpty(dto.CompanyName))
                company.CompanyName = dto.CompanyName;

            if (!string.IsNullOrEmpty(dto.Address))
                company.Address = dto.Address;



          
            await _ctx.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteUserAsync(int id)
        {
            var user = await _ctx.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null)
                return false;

            _ctx.Users.Remove(user);

                await _ctx.SaveChangesAsync();
            return true;
        }

     


        }
    }
