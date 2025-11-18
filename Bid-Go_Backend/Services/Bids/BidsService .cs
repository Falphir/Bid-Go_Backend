using Bid_Go_Backend.Data;
using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.DTOs;
using Bid_Go_Backend.Data.Models.Enums;
using Bid_Go_Backend.Repositories.Interfaces;
using Bid_Go_Backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Bid_Go_Backend.Services
{
    /// <summary>
    /// Service implementing bid-related business logic.
    /// </summary>
    public class BidsService : IBidsService
    {
        private readonly IBidsRepository _repo;
        private readonly BidGoDbContext _ctx;

        public BidsService(IBidsRepository repo, BidGoDbContext ctx)
        {
            _repo = repo;
            _ctx = ctx;
        }

        /// <summary>
        /// Add a new bid for the specified transport request on behalf of a driver.
        /// </summary>
        /// <remarks>
        /// Validates request and bid constraints before delegating persistence to the repository.
        /// </remarks>
        public async Task<(bool Success, string Message, Bid? Bid)> AddBidAsync(int driverId, AddBidDTO bidDto)
        {
            var request = await _ctx.TransportRequests.AsNoTracking()
                .FirstOrDefaultAsync(tr => tr.TransportRequestId == bidDto.TransportRequestId);

            if (request == null)
                return (false, "Transport request not found.", null);

            if (request.Status != ERequestStatus.Active)
                return (false, "Cannot place a bid on a transport request that is not open.", null);

            if (bidDto.Value <= 0)
                return (false, "Bid value must be greater than zero.", null);

            if (bidDto.Value > request.MaxPrice)
                return (false, "Bid value cannot exceed the maximum price of the transport request.", null);

            if (bidDto.DeliveryDeadline <= request.PickupDate)
                return (false, "The bid's delivery deadline must be later than the pickup date.", null);

            if (bidDto.DeliveryDeadline > request.DeliveryDate)
                return (false, "The bid's delivery deadline cannot be later than the delivery date.", null);

            var existingBids = await _repo.GetByTransportRequestAsync(bidDto.TransportRequestId);
            if (existingBids.Any(b => b.DriverId == driverId && b.Status != EBidStatus.Canceled && b.Status != EBidStatus.Rejected))
                return (false, "Driver already has an active bid for this transport request.", null);

            var bid = new Bid
            {
                DriverId = driverId,
                TransportRequestId = bidDto.TransportRequestId,
                Value = bidDto.Value,
                DeliveryDeadline = bidDto.DeliveryDeadline,
                Status = EBidStatus.Pendent
            };

            var created = await _repo.CreateAsync(bid);
            return (true, string.Empty, created);
        }

        /// <summary>
        /// Update an existing bid.
        /// </summary>
        public async Task<(bool Success, string Message, Bid? Bid)> UpdateBidAsync(int id, BidUpdateDTO updateDto)
        {
            var bid = await _repo.GetByIdAsync(id);
            if (bid == null)
                return (false, "Bid not found.", null);

            if (bid.Status != EBidStatus.Pendent)
                return (false, "Only pending bids can be updated.", null);

            var request = await _ctx.TransportRequests
           .AsNoTracking()
           .FirstOrDefaultAsync(tr => tr.TransportRequestId == bid.TransportRequestId);

            if (request == null)
                return (false, "Associated transport request not found.", null);

            if (updateDto.Value <= 0)
                return (false, "Bid value must be greater than zero.", null);

            if (updateDto.Value > request.MaxPrice)
                return (false, "Bid value cannot exceed the maximum price of the transport request.", null);

            if (updateDto.DeliveryDeadline <= request.PickupDate)
                return (false, "The bid's delivery deadline must be later than the pickup date.", null);

            if (updateDto.DeliveryDeadline > request.DeliveryDate)
                return (false, "The bid's delivery deadline cannot be later than the delivery date.", null);

            bid.Value = updateDto.Value;
            bid.DeliveryDeadline = updateDto.DeliveryDeadline;

            await _repo.UpdateAsync(bid);
            return (true, string.Empty, bid);
        }

        /// <summary>
        /// Cancel a pending bid.
        /// </summary>
        public async Task<(bool Success, string Message)> CancelBidAsync(int id)
        {
            var bid = await _repo.GetByIdAsync(id);
            if (bid == null)
                return (false, "Bid not found.");

            if (bid.Status != EBidStatus.Pendent)
                return (false, "Only pending bids can be canceled.");

            bid.Status = EBidStatus.Canceled;
            await _repo.UpdateAsync(bid);
            return (true, "Bid canceled successfully.");
        }

        /// <summary>
        /// Get a bid by id.
        /// </summary>
        public Task<Bid?> GetBidByIdAsync(int id) => _repo.GetByIdAsync(id);

        /// <summary>
        /// Get bids for a transport request.
        /// </summary>
        public Task<List<Bid>> GetBidsByTransportRequestAsync(int transportRequestId) =>
            _repo.GetByTransportRequestAsync(transportRequestId);

        /// <summary>
        /// Get bids for a transport request filtered by status.
        /// </summary>
        public Task<IEnumerable<Bid>> GetBidsByTransportRequestAndStatusAsync(int transportRequestId, EBidStatus status) =>
            _repo.GetByTransportRequestAndStatusAsync(transportRequestId, status);

        /// <summary>
        /// Get active (pending) bids with optional ordering.
        /// </summary>
        public Task<List<Bid>> GetActiveBidsAsync(int transportRequestId, string? orderBy = "value", bool descending = false) =>
            _repo.GetActiveBidsAsync(transportRequestId, orderBy, descending);

        public Task<List<Bid>> GetBidsByDriverId(int driverId) =>
             _repo.GetBidsByDriverId(driverId);

    }
}