using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bid_Go_Backend.Services.Interfaces
{
    public interface IProfileService
    {
        Task<User?> GetProfileAsync(int id);
        Task<bool> UpdateDriverProfileAsync(int id, DriverProfileUpdateDTO dto);

        Task<bool> UpdateCompanyProfileAsync(int id, CompanyProfileUpdateDTO dto);
        Task<bool> ChangePasswordAsync(int id, string currentPassword, string newPassword);
        Task<bool> DeactivateUserAsync(int id);
    }
}
