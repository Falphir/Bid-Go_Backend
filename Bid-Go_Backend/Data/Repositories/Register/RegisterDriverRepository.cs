using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bid_Go_Backend.Data.Repositories.Register
{
    public class RegisterDriverRepository : IRegisterDriverRepository
    {
        private readonly BidGoDbContext _context;

        public RegisterDriverRepository(BidGoDbContext context)
        {
            _context = context;
        }

        public async Task<Driver?> GetByEmailAsync(string email)
              => await _context.Drivers.FirstOrDefaultAsync(c => c.Email == email);

        public async Task<Driver?> GetByPhoneAsync(int phone)
            => await _context.Drivers.FirstOrDefaultAsync(c => c.PhoneNumber == phone);

        public async Task<Driver?> GetByNIFAsync(int nif)
            => await _context.Drivers.FirstOrDefaultAsync(c => c.NIF == nif);

        public async Task<Driver> CreateAsync(Driver driver)
        {
            _context.Drivers.Add(driver);
            await _context.SaveChangesAsync();
            return driver;
        }
    }
}
