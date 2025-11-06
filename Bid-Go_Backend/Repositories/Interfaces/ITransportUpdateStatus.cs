using Bid_Go_Backend.Data.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Bid_Go_Backend.Repositories.Interfaces
{
    public interface ITransportUpdateStatus
    {
        Task<TransportRequest?> GetTransportRequestWithBidsAsync(int id);
        Task<User?> GetUserByIdAsync(int userId);
        void UpdateTransportRequest(TransportRequest request);
        void UpdateBids(IEnumerable<Bid> bids);
        Task SaveChangesAsync();
    }
}
