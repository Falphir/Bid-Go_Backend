using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.DTOs;
using Bid_Go_Backend.Data.Models.Enums;

namespace Bid_Go_Backend.Repositories.Interfaces
{
    public interface ITransportRequestRepository
    {
        Task<TransportRequest> CreateAsync(TransportRequest transportRequest);
        Task<TransportRequest> UpdateAsync(int id, TransportRequest request);
        Task<bool> DeleteAsync(int id);
        Task<TransportRequest> GetByIdAsync(int id);
        Task<TransportRequest> GetRequestWithBidsByIdAsync(int id);
        Task<List<TransportRequest>> GetAllByCompanyAsync(int companyId);

    }
}
