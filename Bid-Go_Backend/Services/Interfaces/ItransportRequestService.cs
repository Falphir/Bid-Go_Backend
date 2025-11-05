using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.DTOs;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bid_Go_Backend.Services.Interfaces
{
    public interface ITransportRequestService
    {
        Task<TransportRequest> CreateAsync(CreateTransportRequestDTO dto, IFormFile imageFile);
        Task<TransportRequest?> UpdateAsync(int id, UpdateTransportRequestDTO dto, IFormFile? imageFile);
        Task<bool> DeleteAsync(int id);
        Task<TransportRequest?> GetByIdAsync(int id);
        Task<List<TransportRequest>> GetByCompanyAsync(int companyId);
    }
}
