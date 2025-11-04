
using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.DTOs;

namespace Bid_Go_Backend.Repositories.Interface
{
    public interface IProfileRepository
    {
        Task<User?> GetUserByIdAsync(int id);
        Task<bool> UpdateDriverAsync(Driver driver);
        Task<bool> UpdateCompanyAsync(Company company);
        Task<bool> HasActiveBidsAsync(int driverId);
        Task<bool> HasActiveTransportRequestsAsync(int companyId);
        Task SaveChangesAsync();
    }
}