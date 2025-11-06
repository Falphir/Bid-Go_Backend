using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Repositories.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bid_Go.Tests.Integration.Utils
{
    public class FakeRegisterCompanyRepository : IRegisterCompanyRepository
    {
        private readonly ConcurrentDictionary<string, Company> _companies = new();
        private int _nextId = 1;

        public FakeRegisterCompanyRepository()
        {
            var company = new Company
            {
                Id = _nextId++,
                Name = "Default Admin",
                CompanyName = "BidGo Test Company",
                Address = "Rua de Teste 123",
                Email = "company@test.com",
                Password = BCrypt.Net.BCrypt.HashPassword("123456"),
                PhoneNumber = 987654321,
                NIF = 999888777
            };

            _companies[company.Email] = company;
        }

        public Task<Company?> GetByEmailAsync(string email)
        {
            _companies.TryGetValue(email, out var company);
            return Task.FromResult(company);
        }

        public Task<Company?> GetByPhoneAsync(int phone)
        {
            var company = _companies.Values.FirstOrDefault(c => c.PhoneNumber == phone);
            return Task.FromResult(company);
        }

        public Task<Company?> GetByNIFAsync(int nif)
        {
            var company = _companies.Values.FirstOrDefault(c => c.NIF == nif);
            return Task.FromResult(company);
        }

        public Task<Company> CreateAsync(Company company)
        {
            company.Id = _nextId++;
            _companies[company.Email] = company;
            return Task.FromResult(company);
        }
    }
}
