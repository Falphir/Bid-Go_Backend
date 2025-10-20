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

            driver.Name = dto.Name;
            driver.PhoneNumber = dto.PhoneNumber;
            driver.DriverLicense = dto.DriverLicense;
            driver.Insurance = dto.Insurance;

            _ctx.Drivers.Update(driver);
            await _ctx.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateCompanyAsync(int id, CompanyProfileDTO dto)
        {
            var company = await _ctx.Companies.FindAsync(id);
            if (company == null)
                return false;

            company.Name = dto.Name;
            company.PhoneNumber = dto.PhoneNumber;
            company.CompanyName = dto.CompanyName;
            company.Address = dto.Address;

            _ctx.Companies.Update(company);
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
