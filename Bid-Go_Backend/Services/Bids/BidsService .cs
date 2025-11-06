using Bid_Go_Backend.Data;
using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.DTOs;
using Bid_Go_Backend.Data.Models.Enums;
using Bid_Go_Backend.Repositories.Interfaces;
using Bid_Go_Backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Bid_Go_Backend.Services
{
    public class BidsService : IBidsService
    {
        private readonly IBidsRepository _repo;
        private readonly BidGoDbContext _ctx;

        public BidsService(IBidsRepository repo, BidGoDbContext ctx)
        {
            _repo = repo;
            _ctx = ctx;
        }

        public async Task<(bool Success, string Message, Bid? Bid)> AddBidAsync(AddBidDTO bidDto)
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
            if (existingBids.Any(b => b.DriverId == bidDto.DriverId && b.Status != EBidStatus.Canceled && b.Status != EBidStatus.Rejected))
                return (false, "Driver already has an active bid for this transport request.", null);

            var bid = new Bid
            {
                DriverId = bidDto.DriverId,
                TransportRequestId = bidDto.TransportRequestId,
                Value = bidDto.Value,
                DeliveryDeadline = bidDto.DeliveryDeadline,
                Status = EBidStatus.Pendent
            };

            var created = await _repo.CreateAsync(bid);
            return (true, string.Empty, created);
        }

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

        public Task<Bid?> GetBidByIdAsync(int id) => _repo.GetByIdAsync(id);

        public Task<List<Bid>> GetBidsByTransportRequestAsync(int transportRequestId) =>
            _repo.GetByTransportRequestAsync(transportRequestId);

        public Task<IEnumerable<Bid>> GetBidsByTransportRequestAndStatusAsync(int transportRequestId, EBidStatus status) =>
            _repo.GetByTransportRequestAndStatusAsync(transportRequestId, status);

        public Task<List<Bid>> GetActiveBidsAsync(int transportRequestId, string? orderBy = "value", bool descending = false) =>
            _repo.GetActiveBidsAsync(transportRequestId, orderBy, descending);
    }
}
