using Bid_Go_Backend.Data.Models;

namespace Bid_Go_Backend.Repositories.Interfaces
{
    public interface IAutomaticSelectionAlgorithmRepository
    {
        Task<TransportRequest?> GetTransportRequestWithBidsAsync(int transportRequestId);
        Task<Dictionary<int, decimal>> GetDriverReputationsAsync(IEnumerable<int> driverIds);
        Task SaveChangesAsync();
    }

}