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
    public class FakeRegisterDriverRepository : IRegisterDriverRepository
    {
        private readonly ConcurrentDictionary<string, Driver> _drivers = new();
        private int _nextId = 1;

        public FakeRegisterDriverRepository()
        {
            var driver = new Driver
            {
                Id = _nextId++,
                Name = "Default Driver",
                Email = "driver@test.com",
                Password = BCrypt.Net.BCrypt.HashPassword("123456"),
                PhoneNumber = 912345678,
                NIF = 123456789,
                DriverLicense = "https://example.com/license.jpg",
                Insurance = "https://example.com/insurance.jpg"
            };

            _drivers[driver.Email] = driver;
        }

        public Task<Driver?> GetByEmailAsync(string email)
        {
            _drivers.TryGetValue(email, out var driver);
            return Task.FromResult(driver);
        }

        public Task<Driver?> GetByPhoneAsync(int phone)
        {
            var driver = _drivers.Values.FirstOrDefault(d => d.PhoneNumber == phone);
            return Task.FromResult(driver);
        }

        public Task<Driver?> GetByNIFAsync(int nif)
        {
            var driver = _drivers.Values.FirstOrDefault(d => d.NIF == nif);
            return Task.FromResult(driver);
        }

        public Task<Driver> CreateAsync(Driver driver)
        {
            driver.Id = _nextId++;
            _drivers[driver.Email] = driver;
            return Task.FromResult(driver);
        }
    }
}
