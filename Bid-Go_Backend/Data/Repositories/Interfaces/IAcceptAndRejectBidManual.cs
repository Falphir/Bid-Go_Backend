using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.Enums;

namespace Bid_Go_Backend.Data.Repositories.Interfaces
{
    public interface IAcceptAndRejectBidManual
    {
        Task AcceptBidAsync(int id);
        Task<Bid?> GetBidByIdAsync(int id);
        Task<IEnumerable<Bid>> GetBidByTransportRequestAndStatusAsync(int transportRequestId, EBidStatus status);
        Task<List<Bid>> GetBidByTransportRequestAsync(int transportRequestId);
        Task RejectBidAsync(int id);
    }
}