using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.DTOs;
using Bid_Go_Backend.Data.Models.Enums;
using Bid_Go_Backend.Data.Repositories.Interfaces;
using Bid_Go_Backend.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading.Tasks;

namespace Bid_Go_Backend.Services.Transport_Request
{
    public class TransportUpdateStatusService : ITransportUpdateStatusService
    {
        private readonly ITransportUpdateStatus _repository;
        private readonly INotificationRepository _notificationRepo;
        private readonly ILogger<TransportUpdateStatusService> _logger;

        public TransportUpdateStatusService(ITransportUpdateStatus repository, INotificationRepository notificationRepo, ILogger<TransportUpdateStatusService> logger)
        {
            _repository = repository;
            _notificationRepo = notificationRepo;
            _logger = logger;
        }

        public async Task<TransportRequestResponseDTO?> UpdateRequestStatusAsync(int id, int companyID, ERequestStatus newStatus)
        {
            _logger.LogDebug("Updating request {RequestId} to {Status} by user {UserId}", id, newStatus, companyID);

            var request = await _repository.GetTransportRequestWithBidsAsync(id);
            if (request == null)
            {
                return null;
            }

            var user = await _repository.GetUserByIdAsync(companyID);
            if (user == null)
            {
                throw new System.InvalidOperationException("User not found.");
            }

            bool isValidTransition = false;
            string userRole;

            if (user is Company)
            {
                userRole = "Company";
                isValidTransition = request.Status switch
                {
                    ERequestStatus.Active => newStatus is ERequestStatus.Pending or ERequestStatus.Canceled,
                    ERequestStatus.Pending => newStatus == ERequestStatus.WaitingPickup,
                    _ => false
                };
            }
            else if (user is Driver)
            {
                userRole = "Driver";
                isValidTransition = request.Status switch
                {
                    ERequestStatus.WaitingPickup => newStatus == ERequestStatus.InTransit,
                    ERequestStatus.InTransit => newStatus is ERequestStatus.Completed or ERequestStatus.Canceled,
                    _ => false
                };
            }
            else
            {
                throw new System.InvalidOperationException("User type not supported.");
            }

            if (!isValidTransition)
            {
                throw new System.InvalidOperationException($"It is not possible to change the state of '{request.Status}' to '{newStatus}' for the type '{userRole}'.");
            }

            request.Status = newStatus;
            _repository.UpdateTransportRequest(request);

            if (userRole == "Company" && newStatus == ERequestStatus.Canceled)
            {
                var pendingBids = request.Bids
                    .Where(b => b.Status == EBidStatus.Pendent)
                    .ToList();

                foreach (var bid in pendingBids)
                {
                    await _notificationRepo.CreateAsync(
                        bid.DriverId,
                        "The order associated with your bid was cancelled by the company.",
                        ENotificationType.Canceled,
                        bid.BidId,
                        bid.TransportRequestId
                    );

                    await _notificationRepo.SendAsync(
                        bid.DriverId,
                        "The order associated with your bid was cancelled by the company.",
                        ENotificationType.Canceled
                    );

                    bid.Status = EBidStatus.Canceled;
                }

                if (pendingBids.Any())
                {
                    _repository.UpdateBids(pendingBids);
                }
            }

            await _repository.SaveChangesAsync();

            return new TransportRequestResponseDTO
            {
                Origin = request.Origin,
                Destination = request.Destination,
                Package = request.Package,
                PickupDate = request.PickupDate,
                DeliveryDate = request.DeliveryDate,
                Weight = request.Weight,
                Volume = request.Volume,
                Length = request.Length,
                Width = request.Width,
                Height = request.Height,
                Image = request.Image,
                MaxPrice = request.MaxPrice,
                Status = request.Status
            };
        }
    }
}
