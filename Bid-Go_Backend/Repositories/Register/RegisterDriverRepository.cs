using Bid_Go_Backend.Data;
using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bid_Go_Backend.Repositories.Register
{
    /// <summary>
    /// Repository for driver registration related persistence and lookups.
    /// </summary>
    public class RegisterDriverRepository : IRegisterDriverRepository
    {
        private readonly BidGoDbContext _context;

        public RegisterDriverRepository(BidGoDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Find a driver by email.
        /// </summary>
        public async Task<Driver?> GetByEmailAsync(string email)
              => await _context.Drivers.FirstOrDefaultAsync(c => c.Email == email);

        /// <summary>
        /// Find a driver by phone number.
        /// </summary>
        public async Task<Driver?> GetByPhoneAsync(int phone)
            => await _context.Drivers.FirstOrDefaultAsync(c => c.PhoneNumber == phone);

        /// <summary>
        /// Find a driver by tax id (NIF).
        /// </summary>
        public async Task<Driver?> GetByNIFAsync(int nif)
            => await _context.Drivers.FirstOrDefaultAsync(c => c.NIF == nif);

        /// <summary>
        /// Create a driver and persist it.
        /// </summary>
        public async Task<Driver> CreateAsync(Driver driver)
        {
            _context.Drivers.Add(driver);
            await _context.SaveChangesAsync();
            return driver;
        }
    }
}
