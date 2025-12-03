using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.DTOs;
using Bid_Go_Backend.Data.Models.Enums;
using Bid_Go_Backend.Repositories.Interfaces;
using Bid_Go_Backend.Services.Interfaces;
using Microsoft.OpenApi.Expressions;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;

namespace Bid_Go_Backend.Services.Bids
{
    /// <summary>
    /// Service allowing manual acceptance or rejection of bids by the company.
    /// </summary>
    public class AcceptAndRejectBidManualService : IAcceptAndRejectBidManualService
    {
        private readonly IAcceptAndRejectBidManualRepository _repo;
        private readonly INotificationService _notificationService;

        public AcceptAndRejectBidManualService(IAcceptAndRejectBidManualRepository repo, INotificationService notificationService)
        {
            _repo = repo;
            _notificationService = notificationService;
        }

        /// <summary>
        /// Get a bid by identifier.
        /// </summary>
        public Task<Bid?> GetBidByIdAsync(int id) => _repo.GetByIdAsync(id);

        /// <summary>
        /// List bids for a transport request.
        /// </summary>
        public Task<List<Bid>> GetBidsByTransportRequestAsync(int transportRequestId) =>
            _repo.GetByTransportRequestAsync(transportRequestId);

        /// <summary>
        /// List bids for a transport request filtered by status.
        /// </summary>
        public async Task<List<BidWithDriverDTO>> GetBidsByTransportRequestAndStatusAsync(int transportRequestId, EBidStatus status)
        {
            var bids = await _repo.GetByTransportRequestAndStatusAsync(transportRequestId, status);

            return bids.Select(b => new BidWithDriverDTO
            {
                BidId = b.BidId,
                Value = b.Value,
                DriverId = b.DriverId,
                DriverName = b.Driver?.Name,
                DriverEmail = b.Driver?.Email,
                Deadline = b.DeliveryDeadline
            }).ToList();
        }

        /// <summary>
        /// Accept a bid, reject other pending bids, and send notifications.
        /// </summary>
        public async Task AcceptBidAsync(int id)
        {
            var bid = await _repo.GetByIdAsync(id);
            if (bid == null)
                throw new Exception("Bid not found.");

            if (bid.Status != EBidStatus.Pendent)
                throw new Exception("The bid is not pending and cannot be accepted");

            if (bid.TransportRequest == null)
                throw new Exception("Associated transport request not found");

            if (bid.TransportRequest.Status != ERequestStatus.Active)
                throw new Exception("The transport request is not active.");

            // Ensure there is no previously accepted bid
            bool alreadyAccepted = (await _repo.GetByTransportRequestAndStatusAsync(bid.TransportRequestId, EBidStatus.Accepted)).Any();
            if (alreadyAccepted)
                throw new Exception("There is already an accepted bid for this request");

            // Accept the bid
            bid.Status = EBidStatus.Accepted;
            bid.TransportRequest.Status = ERequestStatus.Pending;
            bid.TransportRequest.SelectedBidId = bid.BidId;

            // Reject other pending bids
            var otherBids = (await _repo.GetByTransportRequestAsync(bid.TransportRequestId))
                            .Where(b => b.BidId != id && b.Status == EBidStatus.Pendent)
                            .ToList();

            foreach (var other in otherBids)
                other.Status = EBidStatus.Rejected;

            await _repo.SaveChangesAsync();

            // Notifications
            await _notificationService.CreateAndSendAsync(
            bid.DriverId,
             $"Your bid for the transport request #{bid.TransportRequestId} was accepted.",
            ENotificationType.Accepted,
            bid.BidId,
            bid.TransportRequestId
        );

            foreach (var other in otherBids)
            {
                await _notificationService.CreateAndSendAsync(
                    other.DriverId,
                     $"Your bid for the transport request #{bid.TransportRequestId} was rejected.",
                    ENotificationType.Rejected,
                    other.BidId,
                    other.TransportRequestId
                );
            }
        }

        /// <summary>
        /// Reject a bid and notify the driver.
        /// </summary>
        public async Task RejectBidAsync(int id)
        {
            var bid = await _repo.GetByIdAsync(id);
            if (bid == null)
                throw new Exception("Bid not found.");

            if (bid.Status != EBidStatus.Pendent)
                throw new Exception("The bid is not pending and cannot be rejected");

            if (bid.TransportRequest == null)
                throw new Exception("Associated transport request not found");

            if (bid.TransportRequest.Status != ERequestStatus.Active)
                throw new Exception("The transport request is not active.");

            bid.Status = EBidStatus.Rejected;
            await _repo.SaveChangesAsync();

            await _notificationService.CreateAndSendAsync(
                   bid.DriverId,
                   $"Your bid for the transport request #{bid.TransportRequestId} was rejected.",
                   ENotificationType.Rejected,
                   bid.BidId,
                   bid.TransportRequestId
               );
        }
    }
}