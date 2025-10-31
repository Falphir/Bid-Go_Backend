using Bid_Go_Backend.Data;
using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.DTOs.CompanyDTOs;
using Bid_Go_Backend.Data.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bid_Go_Backend.Services
{
    public class RegisterCompanyRepository : IRegisterCompanyRepository
    {
        private readonly BidGoDbContext _context;

        public RegisterCompanyRepository(BidGoDbContext context)
        {
            _context = context;
        }

        public async Task<Company?> GetByEmailAsync(string email)
              => await _context.Companies.FirstOrDefaultAsync(c => c.Email == email);

        public async Task<Company?> GetByPhoneAsync(int phone)
            => await _context.Companies.FirstOrDefaultAsync(c => c.PhoneNumber == phone);

        public async Task<Company?> GetByNIFAsync(int nif)
            => await _context.Companies.FirstOrDefaultAsync(c => c.NIF == nif);

        public async Task<Company> CreateAsync(Company company)
        {
            _context.Companies.Add(company);
            await _context.SaveChangesAsync();
            return company;
        }
    }
}