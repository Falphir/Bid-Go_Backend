using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bid_Go_Backend.Data.Models;

namespace Bid_Go_Backend.Repositories.Interfaces
{
    public interface IRegisterCompanyRepository
    {
        Task<Company> CreateAsync(Company company);
        Task<Company?> GetByEmailAsync(string email);
        Task<Company?> GetByNIFAsync(int NIF);
        Task<Company?> GetByPhoneAsync(int PhoneNumber);
    }
}
