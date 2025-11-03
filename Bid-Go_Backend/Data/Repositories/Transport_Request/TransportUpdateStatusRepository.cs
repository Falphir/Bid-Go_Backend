using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.DTOs;
using Bid_Go_Backend.Data.Models.Enums;
using Bid_Go_Backend.Data.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Bid_Go_Backend.Data.Repositories.Transport_Request
{
    public class TransportUpdateStatusRepository : ITransportUpdateStatus
    {
        private readonly BidGoDbContext _context;
        private readonly INotificationRepository _notificationRepo;
        public TransportUpdateStatusRepository(BidGoDbContext context, INotificationRepository notificationRepo)
        {
            _context = context;
            _notificationRepo = notificationRepo;
        }

        public async Task<TransportRequestResponseDTO> UpdateRequestStatusAsync(int id, int companyID, ERequestStatus newStatus)
        {
            var request = await _context.TransportRequests
                 .Include(r => r.Bids)
                 .FirstOrDefaultAsync(r => r.TransportRequestId == id);
            if (request == null)
            {
                throw new Exception("Transport request not found.");
            }

            var user = await _context.Users.FindAsync(companyID);
            if (user == null)
            {
                throw new InvalidOperationException("User not found.");
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
                throw new InvalidOperationException("User type not supported.");
            }

            if (!isValidTransition)
            {
                throw new InvalidOperationException(
                    $"It is not possible to change the state of '{request.Status}' to '{newStatus}' for the type '{userRole}'.");
            }

            request.Status = newStatus;
            _context.TransportRequests.Update(request);

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
                    _context.Bids.UpdateRange(pendingBids);
            }

            await _context.SaveChangesAsync();

          
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
