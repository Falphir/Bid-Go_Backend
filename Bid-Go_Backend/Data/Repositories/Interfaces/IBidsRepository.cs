using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.Enums;

namespace Bid_Go_Backend.Data.Repositories.Interfaces
{
    public interface IBidsRepository
    {
        Task<Bid?> GetByIdAsync(int id);
        Task<Bid> CreateAsync(Bid bid);
        Task<Bid> UpdateAsync(Bid bid);
        Task<List<Bid>> GetByTransportRequestAsync(int transportRequestId);
        Task<IEnumerable<Bid>> GetByTransportRequestAndStatusAsync(int transportRequestId, EBidStatus status);
        Task<List<Bid>> GetActiveBidsAsync(int transportRequestId, string? orderBy = "value", bool descending = false);
    }

}