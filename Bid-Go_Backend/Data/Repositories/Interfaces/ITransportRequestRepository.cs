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
        Task<TransportRequest> CreateAsync(TransportRequest transportRequest);
        Task<TransportRequest?> UpdateAsync(int id, TransportRequest transportRequest);
        Task<bool> DeleteAsync(int id);
        Task<TransportRequest> GetByIdAsync(int id);
        Task<List<TransportRequest>> GetAllByCompanyAsync(int companyId);


    }
}
