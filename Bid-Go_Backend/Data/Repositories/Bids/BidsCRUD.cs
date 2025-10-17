using Bid_Go_Backend.Data;
using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.DTOs;
using Bid_Go_Backend.Data.Models.Enums;
using Bid_Go_Backend.Repositories.Interface;
using Microsoft.EntityFrameworkCore;


namespace Bid_Go_Backend.Repositories.BidRepo
{
    public class BidsCRUD : IBidCRUD
    {
        private readonly BidGoDbContext _ctx;

        public BidsCRUD(BidGoDbContext ctx)
        {
            _ctx = ctx;
        }


        public async Task<List<Bid>> GetActiveBidsByTransportRequestAsync(int transportRequestId, string? orderBy = "value", bool descending = false)
        {
            var query = _ctx.Bids
                .Include(b => b.Driver)
                .AsNoTracking()
                .Where(b => b.TransportRequestId == transportRequestId &&
                            b.Status == EBidStatus.Pendent);

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

