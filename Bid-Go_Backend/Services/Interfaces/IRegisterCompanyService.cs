using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.DTOs.CompanyDTOs;

namespace Bid_Go_Backend.Services.Interfaces
{
    public interface IRegisterCompanyService
    {
        Task<(bool Success, string? Error, Company? Company)> RegisterAsync(RegisterCompanyDTO dto);
    }
}