using Bid_Go_Backend.Data.Models;

namespace Bid_Go_Backend.Data.Repositories.Interfaces
{
    public interface IRegisterDriverRepository
    {
        Task<Driver> CreateAsync(Driver driver);
        Task<Driver?> GetByEmailAsync(string email);
        Task<Driver?> GetByNIFAsync(int nif);
        Task<Driver?> GetByPhoneAsync(int phone);
    }
}