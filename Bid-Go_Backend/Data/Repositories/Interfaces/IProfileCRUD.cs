
using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.DTOs;

namespace Bid_Go_Backend.Repositories.Interface
{
    public interface IProfileCrud
    {
        Task<User?> GetUserByIdAsync(int id);

        Task<bool> UpdateDriverAsync(int id, DriverProfileDTO dto);
        Task<bool> UpdateCompanyAsync(int id, CompanyProfileDTO dto);
        Task<bool> DeactivateUserAsync(int id);

    }
}