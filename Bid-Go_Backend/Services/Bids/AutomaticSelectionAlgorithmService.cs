using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.Enums;
using Bid_Go_Backend.Repositories.Interfaces;
using Bid_Go_Backend.Services.Interfaces;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
using System.Security.Cryptography;

namespace Bid_Go_Backend.Services.Bids
{
    /// <summary>
    /// Service that runs the automatic bid selection algorithm based on price and reputation.
    /// </summary>
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

        /// <summary>
        /// Execute the algorithm and accept the best eligible bid, sending notifications accordingly.
        /// </summary>
        /// <param name="transportRequestId">Transport request identifier.</param>
        /// <returns>Tuple with success flag, error message when any, and the selected bid.</returns>
        public async Task<(bool Success, string? Message, Bid? SelectedBid)> ExecuteAsync(int transportRequestId)
        {
            var transportRequest = await _repo.GetTransportRequestWithBidsAsync(transportRequestId);
            if (transportRequest == null)
                return (false, "Transport request not found.", null);

            if (!transportRequest.IsAutomaticSelectionEnabled)
                return (false, "Automatic selection is not enabled.", null);

            if (transportRequest.BiddingEndDate > DateTime.UtcNow)
                return (false, "Bidding has not finished yet.", null);

            // Only active requests can proceed
            if (transportRequest.Status != ERequestStatus.Active)
                return (false, "The transport request is not active.", null);

            // Only when there is no accepted bid already
            if (transportRequest.Bids.Any(b => b.Status == EBidStatus.Accepted))
                return (false, "There is already an accepted bid for this request.", null);

            if (!transportRequest.Bids.Any())
            {
                await _notificationService.CreateAndSendAsync(
                   transportRequest.CompanyId, $"The automatic selection process has completed, but no bids were submitted for request #{transportRequest.TransportRequestId}. Automatic selection could not be performed.", ENotificationType.New_message, null, transportRequest.TransportRequestId);


                transportRequest.Status = ERequestStatus.Canceled;

                await _repo.SaveChangesAsync();

                return (false, "No bids submitted.", null);
            }
                

            // Reputation filter (>=3)
            var driverIds = transportRequest.Bids.Select(b => b.DriverId).Distinct();
            var reputations = await _repo.GetDriverReputationsAsync(driverIds);

            var eligibleBids = transportRequest.Bids
                .Where(b => reputations.GetValueOrDefault(b.DriverId, 0) >= 3)
                .ToList();

            if (!eligibleBids.Any())
            {
                await _notificationService.CreateAndSendAsync(
                  transportRequest.CompanyId, $"The automatic selection process has completed, but no eligible bids were submitted for request #{transportRequest.TransportRequestId}. Manual selection required.", ENotificationType.New_message, null, transportRequest.TransportRequestId);

                await _repo.SaveChangesAsync();

                return (false, "No eligible bids.", null);
            }
                

            // Lowest price, then highest reputation, then smallest id
            var minPrice = eligibleBids.Min(b => b.Value);
            var lowestBids = eligibleBids.Where(b => b.Value == minPrice).ToList();
            var maxReputation = lowestBids.Max(b => reputations.GetValueOrDefault(b.DriverId, 0));
            var bestBids = lowestBids.Where(b => reputations.GetValueOrDefault(b.DriverId, 0) == maxReputation).ToList();
            var selectedBid = bestBids.Count == 1 ? bestBids.First() : bestBids.OrderBy(b => b.BidId).First();

            if (selectedBid.Status != EBidStatus.Pendent)
                return (false, "The selected bid is not pending and cannot be accepted.", null);
                

            // Update states
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
                    other.DriverId, $"Your bid for the transport request #{transportRequest.TransportRequestId} was rejected.", ENotificationType.Rejected, other.BidId, transportRequest.TransportRequestId);
            }

            await _notificationService.CreateAndSendAsync(
                selectedBid.DriverId, $"Your bid for the transport request #{transportRequest.TransportRequestId} was accepted.", ENotificationType.Accepted, selectedBid.BidId, transportRequest.TransportRequestId);

            await _notificationService.CreateAndSendAsync(
                transportRequest.CompanyId, $"The automatic selection process has completed. A winning bid has been chosen for request #{transportRequest.TransportRequestId}.", ENotificationType.New_message, selectedBid.BidId, transportRequest.TransportRequestId);

            await _repo.SaveChangesAsync();

            return (true, null, selectedBid);
        }
    }

}
