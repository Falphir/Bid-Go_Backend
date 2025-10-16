using Bid_Go_Backend.Data.Models;

namespace Bid_Go_Backend.Data.Repositories.Interfaces
{
    public interface IAutomaticSelectionAlgorithmRepository
    {
        Task<Bid?> ExecuteAutomaticSelectionAsync(int transportRequestId);
        Task<IEnumerable<Bid>> GetEligibleBidsAsync(int transportRequestId);
    }
}