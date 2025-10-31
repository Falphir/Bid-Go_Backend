using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.Enums;

namespace Bid_Go_Backend.Data.Repositories.Interfaces
{
    public interface IBidsCRUD
    {
        Task<bool> CancelBidAsync(int id);
        Task<Bid> CreateBidAsync(Bid bid);
        Task<List<Bid>> GetActiveBidsByTransportRequestAsync(int transportRequestId, string? orderBy = "value", bool descending = false);
        Task<Bid?> GetBidByIdAsync(int id);
        Task<IEnumerable<Bid>> GetBidByTransportRequestAndStatusAsync(int transportRequestId, EBidStatus status);
        Task<List<Bid>> GetBidByTransportRequestAsync(int transportRequestId);
        Task<Bid?> UpdateBidAsync(int id, Bid bid);
    }
}