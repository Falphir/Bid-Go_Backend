
using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.DTOs;

namespace Bid_Go_Backend.Repositories.Interface
{
    public interface IProfileCrud
    {
        Task<User?> GetUserByIdAsync(int id);

        Task UpdateDriverAsync(int id, DriverProfileDTO dto);
        Task UpdateCompanyAsync(int id, CompanyProfileDTO dto);
        Task<bool> DeleteUserAsync(int id);

    }
}