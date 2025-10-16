using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.DTOs;

namespace Bid_Go_Backend.Data.Repositories.Interfaces
{
    public interface ITransportRequestRepository
    {
        Task<TransportRequest> UpdateRequestStatusAsync(int id, RequestStatusDTO dto);
    }
}
