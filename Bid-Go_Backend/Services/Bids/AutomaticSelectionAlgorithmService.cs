using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.Enums;
using Bid_Go_Backend.Repositories.Interfaces;
using Bid_Go_Backend.Services.Interfaces;
using System.Security.Cryptography;

namespace Bid_Go_Backend.Services.Bids
{
    public class AutomaticSelectionAlgorithmService : IAutomaticSelectionAlgorithmService
    {
        private readonly IAutomaticSelectionAlgorithmRepository _repo;
        private readonly INotificationService _notificationService;

        public AutomaticSelectionAlgorithmService(
            IAutomaticSelectionAlgorithmRepository repo,
            INotificationService notificationService)
        {
            _repo = repo;
            _notificationService = notificationService;
        }

        public async Task<(bool Success, string? Message, Bid? SelectedBid)> ExecuteAsync(int transportRequestId)
        {
            var transportRequest = await _repo.GetTransportRequestWithBidsAsync(transportRequestId);
            if (transportRequest == null)
                return (false, "Transport request not found.", null);

            if (!transportRequest.IsAutomaticSelectionEnabled)
                return (false, "Automatic selection is not enabled.", null);

            if (transportRequest.BiddingEndDate > DateTime.UtcNow)
                return (false, "Bidding has not finished yet.", null);

           /// if (transportRequest.Status == ERequestStatus.Canceled)
              ///  return (false, "The transport request is canceled.", null);

            if (transportRequest.Status != ERequestStatus.Active)
                return (false, "The transport request is not active.", null);



            if (transportRequest.Bids.Any(b => b.Status == EBidStatus.Accepted))
                return (false, "There is already an accepted bid for this request.", null);



            if (!transportRequest.Bids.Any())
                return (false, "No bids submitted.", null);

            // Reputações
            var driverIds = transportRequest.Bids.Select(b => b.DriverId).Distinct();
            var reputations = await _repo.GetDriverReputationsAsync(driverIds);

            var eligibleBids = transportRequest.Bids
                .Where(b => reputations.GetValueOrDefault(b.DriverId, 0) >= 3)
                .ToList();

            if (!eligibleBids.Any())
                return (false, "No eligible bids.", null);

            // Seleção de bid
            var minPrice = eligibleBids.Min(b => b.Value);
            var lowestBids = eligibleBids.Where(b => b.Value == minPrice).ToList();
            var maxReputation = lowestBids.Max(b => reputations.GetValueOrDefault(b.DriverId, 0));
            var bestBids = lowestBids.Where(b => reputations.GetValueOrDefault(b.DriverId, 0) == maxReputation).ToList();
            var selectedBid = bestBids.Count == 1 ? bestBids.First() : bestBids.OrderBy(b => b.BidId).First();

            if (selectedBid.Status != EBidStatus.Pendent)
                return (false, "The selected bid is not pending and cannot be accepted.", null);

            // Atualizar estado
            transportRequest.SelectedBidId = selectedBid.BidId;
            selectedBid.Status = EBidStatus.Accepted;
            transportRequest.Status = ERequestStatus.Pending;

            var otherBids = transportRequest.Bids
                .Where(b => b.BidId != selectedBid.BidId && b.Status == EBidStatus.Pendent)
                .ToList();

            foreach (var other in otherBids)
            {
                other.Status = EBidStatus.Rejected;
                await _notificationService.CreateAndSendAsync(
                    other.DriverId, "Your bid was rejected.", ENotificationType.Rejected, other.BidId, transportRequest.TransportRequestId);
            }

            await _notificationService.CreateAndSendAsync(
                selectedBid.DriverId, "Your bid was accepted.", ENotificationType.Accepted, selectedBid.BidId, transportRequest.TransportRequestId);

            await _repo.SaveChangesAsync();

            return (true, null, selectedBid);
        }
    }

}
