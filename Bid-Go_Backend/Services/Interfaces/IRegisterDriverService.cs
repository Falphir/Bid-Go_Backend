using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.DTOs;

namespace Bid_Go_Backend.Services.Interfaces
{
    public interface IRegisterDriverService
    {
        Task<(bool Success, string? Error, Driver? Driver)> RegisterAsync(RegisterDriverDTO dto);
    }
}