using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.Enums;

namespace Bid_Go_Backend.Data.Repositories.Interfaces
{
    public interface IAcceptAndRejectBidManualRepository

    {
        Task<Bid?> GetByIdAsync(int id);
        Task<List<Bid>> GetByTransportRequestAsync(int transportRequestId);
        Task<List<Bid>> GetByTransportRequestAndStatusAsync(int transportRequestId, EBidStatus status);
        Task UpdateAsync(Bid bid);
        Task SaveChangesAsync();
    }
}