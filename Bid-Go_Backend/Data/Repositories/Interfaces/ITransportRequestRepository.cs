using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bid_Go_Backend.Data.Repositories.Interfaces
{
    public interface ITransportRequestRepository
    {
        Task<TransportRequest> CreateAsync(CreateTransportRequestDTO dto);
        Task<TransportRequest?> UpdateAsync(int id, UpdateTransportRequestDTO dto);
        Task<bool> DeleteAsync(int id);
    }
}
