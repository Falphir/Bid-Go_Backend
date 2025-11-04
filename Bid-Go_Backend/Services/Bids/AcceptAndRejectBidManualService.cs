using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.Enums;
using Bid_Go_Backend.Data.Repositories.Interfaces;
using Bid_Go_Backend.Repositories.Interface;
using Bid_Go_Backend.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bid_Go_Backend.Services.Bids
{
    public class AcceptAndRejectBidManualService : IAcceptAndRejectBidManualService
    {
        private readonly IAcceptAndRejectBidManualRepository _repo;
        private readonly INotificationService _notificationService;

        public AcceptAndRejectBidManualService(IAcceptAndRejectBidManualRepository repo, INotificationService notificationService)
        {
            _repo = repo;
            _notificationService = notificationService;
        }

        public Task<Bid?> GetBidByIdAsync(int id) => _repo.GetByIdAsync(id);

        public Task<List<Bid>> GetBidsByTransportRequestAsync(int transportRequestId) =>
            _repo.GetByTransportRequestAsync(transportRequestId);

        public Task<List<Bid>> GetBidsByTransportRequestAndStatusAsync(int transportRequestId, EBidStatus status) =>
    _repo.GetByTransportRequestAndStatusAsync(transportRequestId, status);

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

            // Verifica se já existe uma bid aceita
            bool alreadyAccepted = (await _repo.GetByTransportRequestAndStatusAsync(bid.TransportRequestId, EBidStatus.Accepted)).Any();
            if (alreadyAccepted)
                throw new Exception("There is already an accepted bid for this request");

            // Aceita a bid
            bid.Status = EBidStatus.Accepted;

            // Rejeita outras bids pendentes
            var otherBids = (await _repo.GetByTransportRequestAsync(bid.TransportRequestId))
                            .Where(b => b.BidId != id && b.Status == EBidStatus.Pendent)
                            .ToList();

            foreach (var other in otherBids)
                other.Status = EBidStatus.Rejected;

            bid.TransportRequest.Status = ERequestStatus.Pending;

            await _repo.SaveChangesAsync();

            // Notificações
            await _notificationService.CreateAndSendAsync(
            bid.DriverId,
            "Your bid was accepted.",
            ENotificationType.Accepted,
            bid.BidId,
            bid.TransportRequestId
        );

            foreach (var other in otherBids)
            {
                await _notificationService.CreateAndSendAsync(
                    other.DriverId,
                    "Your bid was rejected.",
                    ENotificationType.Rejected,
                    other.BidId,
                    other.TransportRequestId
                );
            }
        }

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
                   "Your bid was rejected.",
                   ENotificationType.Rejected,
                   bid.BidId,
                   bid.TransportRequestId
               );
        }
    }
}