using Bid_Go_Backend.Data;
using Microsoft.EntityFrameworkCore;
using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.Enums;
using Bid_Go_Backend.Repositories.Interfaces;


namespace Bid_Go_Backend.Repositories.Bids
{
    public class AcceptAndRejectBidManualRepository : IAcceptAndRejectBidManualRepository
    {
        private readonly BidGoDbContext _ctx;
        public AcceptAndRejectBidManualRepository(BidGoDbContext ctx) => _ctx = ctx;

        public async Task<Bid?> GetByIdAsync(int id) =>
             await _ctx.Bids
            .Include(b => b.TransportRequest).FirstOrDefaultAsync(b => b.BidId == id);
        

        public Task<List<Bid>> GetByTransportRequestAsync(int transportRequestId) =>
            _ctx.Bids
                .Where(b => b.TransportRequestId == transportRequestId
                         && b.Status != EBidStatus.Canceled
                         && b.Status != EBidStatus.Expired)
                .ToListAsync();

        public Task<List<Bid>> GetByTransportRequestAndStatusAsync(int transportRequestId, EBidStatus status) =>
       _ctx.Bids
           .AsNoTracking()
           .Where(b => b.TransportRequestId == transportRequestId && b.Status == status)
           .ToListAsync();


        public Task UpdateAsync(Bid bid)
        {
            _ctx.Bids.Update(bid);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync() => _ctx.SaveChangesAsync();
    }

}

