using Bid_Go_Backend.Data.Models;

namespace Bid_Go_Backend.Data.Repositories.Interfaces
{
    public interface IAutomaticSelectionAlgorithmRepository
    {
        Task<AutomaticSelectionResult> ExecuteAutomaticSelectionAsync(int transportRequestId);
        Task<IEnumerable<Bid>> GetEligibleBidsAsync(int transportRequestId);
        Task<bool> IsTransportRequestCanceledAsync(int transportRequestId);
    }
}