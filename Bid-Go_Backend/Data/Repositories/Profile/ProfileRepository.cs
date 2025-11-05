using Bid_Go_Backend.Data;
using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.DTOs;
using Bid_Go_Backend.Data.Models.Enums;
using Bid_Go_Backend.Repositories.Interface;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography.X509Certificates;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace Bid_Go_Backend.Repositories.ProfileRepo
{
    public class ProfileRepository : IProfileRepository
    {
        private readonly BidGoDbContext _ctx;

        public ProfileRepository(BidGoDbContext ctx)
        {
            _ctx = ctx;
        }

        public async Task<User?> GetUserByIdAsync(int id) =>
            await _ctx.Users.FirstOrDefaultAsync(u => u.Id == id);

        public async Task<bool> UpdateDriverAsync(Driver driver)
        {
            _ctx.Drivers.Update(driver);
            await _ctx.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateCompanyAsync(Company company)
        {
            _ctx.Companies.Update(company);
            await _ctx.SaveChangesAsync();
            return true;
        }


        public async Task<bool> HasActiveBidsAsync(int driverId)
        {
            return await _ctx.Bids.AnyAsync(b => b.DriverId == driverId &&
                                                 (b.Status == EBidStatus.Pendent || b.Status == EBidStatus.Accepted));
        }

        public async Task<bool> HasActiveTransportRequestsAsync(int companyId)
        {
            return await _ctx.TransportRequests.AnyAsync(tr => tr.CompanyId == companyId &&
                                                               tr.Status != ERequestStatus.Canceled &&
                                                               tr.Status != ERequestStatus.Completed);
        }

        public async Task SaveChangesAsync() => await _ctx.SaveChangesAsync();
    }
}