using Bid_Go_Backend.Data;
using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.DTOs.CompanyDTOs;
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
    /// Repository for company registration related persistence and lookups.
    /// </summary>
    public class RegisterCompanyRepository : IRegisterCompanyRepository
    {
        private readonly BidGoDbContext _context;

        public RegisterCompanyRepository(BidGoDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Find a company by email.
        /// </summary>
        public Task<Company?> GetByEmailAsync(string email)
            => _context.Companies.FirstOrDefaultAsync(c => c.Email == email);

        /// <summary>
        /// Find a company by phone number.
        /// </summary>
        public Task<Company?> GetByPhoneAsync(int phone)
            => _context.Companies.FirstOrDefaultAsync(c => c.PhoneNumber == phone);

        /// <summary>
        /// Find a company by tax id (NIF).
        /// </summary>
        public Task<Company?> GetByNIFAsync(int nif)
            => _context.Companies.FirstOrDefaultAsync(c => c.NIF == nif);

        /// <summary>
        /// Create a company and persist it.
        /// </summary>
        public async Task<Company> CreateAsync(Company company)
        {
            _context.Companies.Add(company);
            await _context.SaveChangesAsync();
            return company;
        }
    }
}