using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.DTOs;
using Bid_Go_Backend.Data.Models.Enums;

namespace Bid_Go_Backend.Data.Repositories.Interfaces
{
    public interface ITransportRequestRepository
    {
        Task<TransportRequest> UpdateRequestStatusAsync(int id, int companyID, ERequestStatus status);
    }
}
