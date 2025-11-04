using Bid_Go_Backend.Data.Models;

namespace Bid_Go_Backend.Services.Interfaces
{
    public interface IAutomaticSelectionAlgorithmService
    {
        Task<(bool Success, string? Message, Bid? SelectedBid)> ExecuteAsync(int transportRequestId);
    }
}