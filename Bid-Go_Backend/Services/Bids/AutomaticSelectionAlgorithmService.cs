using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Repositories.Interfaces;

namespace Bid_Go_Backend.Services.Bids
{
    public class AutomaticSelectionAlgorithmService : IAutomaticSelectionAlgorithmService
    {
        private readonly IAutomaticSelectionAlgorithmRepository _repo;

        public AutomaticSelectionAlgorithmService(IAutomaticSelectionAlgorithmRepository repo)
        {
            _repo = repo;
        }

        public async Task<(bool Success, string? Message, Bid? SelectedBid)> ExecuteAsync(int transportRequestId)
        {
            var result = await _repo.ExecuteAutomaticSelectionAsync(transportRequestId);
            var success = result.SelectedBid != null;
            return (success, result.Message, result.SelectedBid);
        }
    }
}
