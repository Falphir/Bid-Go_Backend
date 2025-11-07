using Bid_Go_Backend.Data;
using Microsoft.EntityFrameworkCore;
using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.Enums;
using Bid_Go_Backend.Repositories.Interfaces;


namespace Bid_Go_Backend.Repositories.Bids
{
    /// <summary>
    /// Repository for manual accept/reject operations and bid retrievals by transport request.
    /// </summary>
    public class AcceptAndRejectBidManualRepository : IAcceptAndRejectBidManualRepository
    {
        private readonly BidGoDbContext _ctx;
        public AcceptAndRejectBidManualRepository(BidGoDbContext ctx) => _ctx = ctx;

        /// <summary>
        /// Get a bid including its transport request.
        /// </summary>
        public async Task<Bid?> GetByIdAsync(int id) =>
             await _ctx.Bids
            .Include(b => b.TransportRequest).FirstOrDefaultAsync(b => b.BidId == id);
        

        /// <summary>
        /// List non-canceled/expired bids for a transport request.
        /// </summary>
        public Task<List<Bid>> GetByTransportRequestAsync(int transportRequestId) =>
            _ctx.Bids
                .Where(b => b.TransportRequestId == transportRequestId
                         && b.Status != EBidStatus.Canceled
                         && b.Status != EBidStatus.Expired)
                .ToListAsync();

        /// <summary>
        /// List bids for a transport request filtered by status.
        /// </summary>
        public Task<List<Bid>> GetByTransportRequestAndStatusAsync(int transportRequestId, EBidStatus status) =>
       _ctx.Bids
           .AsNoTracking()
           .Where(b => b.TransportRequestId == transportRequestId && b.Status == status)
           .ToListAsync();


        /// <summary>
        /// Update a bid entity in the change tracker.
        /// </summary>
        public Task UpdateAsync(Bid bid)
        {
            _ctx.Bids.Update(bid);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Persist tracked changes to the database.
        /// </summary>
        public Task SaveChangesAsync() => _ctx.SaveChangesAsync();
    }

}

