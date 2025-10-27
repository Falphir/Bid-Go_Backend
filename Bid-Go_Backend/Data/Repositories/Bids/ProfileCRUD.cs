using Bid_Go_Backend.Data;
using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Repositories.Interface;
using Microsoft.EntityFrameworkCore;
using Bid_Go_Backend.Data.Models.DTOs;
using System.Security.Cryptography.X509Certificates;
using Bid_Go_Backend.Data.Models.Enums;

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
            if (driver == null) return false;

            bool updated = false;

            if (!string.IsNullOrEmpty(dto.Name)) { driver.Name = dto.Name; updated = true; }
            if (!string.IsNullOrEmpty(dto.Email)) { driver.Email = dto.Email; updated = true; }
            if (dto.PhoneNumber.HasValue) { driver.PhoneNumber = dto.PhoneNumber.Value; updated = true; }
            if (dto.NIF.HasValue) { driver.NIF = dto.NIF.Value; updated = true; }
            if (!string.IsNullOrEmpty(dto.DriverLicense)) { driver.DriverLicense = dto.DriverLicense; updated = true; }
            if (!string.IsNullOrEmpty(dto.Insurance)) { driver.Insurance = dto.Insurance; updated = true; }

            if (!updated) return false;

            await _ctx.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateCompanyAsync(int id, CompanyProfileDTO dto)
        {
            var company = await _ctx.Companies.FindAsync(id);
            if (company == null) return false;

            bool updated = false;

            if (!string.IsNullOrEmpty(dto.Name)) { company.Name = dto.Name; updated = true; }
            if (!string.IsNullOrEmpty(dto.Email)) { company.Email = dto.Email; updated = true; }
            if (dto.PhoneNumber.HasValue) { company.PhoneNumber = dto.PhoneNumber.Value; updated = true; }
            if (dto.NIF.HasValue) { company.NIF = dto.NIF.Value; updated = true; }
            if (!string.IsNullOrEmpty(dto.CompanyName)) { company.CompanyName = dto.CompanyName; updated = true; }
            if (!string.IsNullOrEmpty(dto.Address)) { company.Address = dto.Address; updated = true; }

            if (!updated) return false;

            await _ctx.SaveChangesAsync();
            return true;
        }



        public async Task<bool> DeactivateUserAsync(int id)
        {
            // Buscar usuário
            var user = await _ctx.Users.FindAsync(id);
            if (user == null)
                throw new Exception("User not found.");

            if (!user.IsActive)
                throw new Exception("User is already inactive.");

            // 🔹 Se for Driver, verifica as Bids
            if (user is Driver)
            {
                bool hasActiveBids = await _ctx.Bids
                    .AnyAsync(b => b.DriverId == user.Id &&
                                   (b.Status == EBidStatus.Pendent || b.Status == EBidStatus.Accepted));

                if (hasActiveBids)
                    throw new Exception("Driver cannot be deactivated with pending or accepted bids.");
            }

            // 🔹 Se for Company, verifica os TransportRequests
            else if (user is Company)
            {
                bool hasActiveRequests = await _ctx.TransportRequests
                    .AnyAsync(tr => tr.CompanyId == user.Id &&
                                    tr.Status != ERequestStatus.Canceled &&
                                    tr.Status != ERequestStatus.Completed);

                if (hasActiveRequests)
                    throw new Exception("Company cannot be deactivated with active transport requests.");
            }

            // 🔹 Marcar como inativo
            user.IsActive = false;
            await _ctx.SaveChangesAsync();

            return true;
        }



    }
}
