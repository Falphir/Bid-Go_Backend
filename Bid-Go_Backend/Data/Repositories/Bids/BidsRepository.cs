using Bid_Go_Backend.Data;
using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.DTOs;
using Bid_Go_Backend.Data.Models.Enums;
using Bid_Go_Backend.Data.Repositories.Interfaces;
using Bid_Go_Backend.Repositories.Interface;
using Microsoft.EntityFrameworkCore;


namespace Bid_Go_Backend.Repositories.BidRepo
{
    public class BidsRepository : IBidsRepository
    {
        private readonly BidGoDbContext _ctx;
        public BidsRepository(BidGoDbContext ctx) => _ctx = ctx;

        public async Task<Bid?> GetByIdAsync(int id) =>
            await _ctx.Bids.AsNoTracking().FirstOrDefaultAsync(b => b.BidId == id);

        public async Task<Bid> CreateAsync(Bid bid)
        {
            _ctx.Bids.Add(bid);
            await _ctx.SaveChangesAsync();
            return bid;
        }

        public async Task<Bid> UpdateAsync(Bid bid)
        {
            _ctx.Bids.Update(bid);
            await _ctx.SaveChangesAsync();
            return bid;
        }

        public async Task<List<Bid>> GetByTransportRequestAsync(int transportRequestId) =>
            await _ctx.Bids.AsNoTracking()
                .Where(b => b.TransportRequestId == transportRequestId)
                .ToListAsync();

        public async Task<IEnumerable<Bid>> GetByTransportRequestAndStatusAsync(int transportRequestId, EBidStatus status) =>
            await _ctx.Bids.AsNoTracking()
                .Where(b => b.TransportRequestId == transportRequestId && b.Status == status)
                .ToListAsync();

        public async Task<List<Bid>> GetActiveBidsAsync(int transportRequestId, string? orderBy = "value", bool descending = false)
        {
            var query = _ctx.Bids.Include(b => b.Driver).AsNoTracking()
                .Where(b => b.TransportRequestId == transportRequestId && b.Status == EBidStatus.Pendent);

            query = (orderBy?.ToLower(), descending) switch
            {
                ("deadline", true) => query.OrderByDescending(b => b.DeliveryDeadline),
                ("deadline", false) => query.OrderBy(b => b.DeliveryDeadline),
                ("value", true) => query.OrderByDescending(b => b.Value),
                _ => query.OrderBy(b => b.Value)
            };

            return await query.ToListAsync();
        }
    }

}

