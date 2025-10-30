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

        //Create a new bid
        public async Task<Bid> CreateBidAsync(Bid bid)
        {
            bid.Status = EBidStatus.Pendent;
            _ctx.Bids.Add(bid);
            await _ctx.SaveChangesAsync();
            return bid;
        }

        //Update bid
        public async Task<Bid?> UpdateBidAsync(int id, Bid bid)
        {

            var existingBid = await _ctx.Bids.FindAsync(id);
         
            if(existingBid == null || existingBid.Status != EBidStatus.Pendent)
            {
                return null;
            }

            existingBid.Value = bid.Value;
            existingBid.DeliveryDeadline = bid.DeliveryDeadline;

            await _ctx.SaveChangesAsync();
            return existingBid;

        }

        //Get bid by id
        public async Task<Bid?> GetBidByIdAsync(int id)
        {
            return await _ctx.Bids
       .AsNoTracking()
       .FirstOrDefaultAsync(b => b.BidId == id);
        }



        //Get bids by transport request id
        public async Task<List<Bid>> GetBidByTransportRequestAsync(int transportRequestId)
        {
            return await _ctx.Bids
                .AsNoTracking()
                .Where(b => b.TransportRequestId == transportRequestId
                && b.Status != EBidStatus.Canceled
                && b.Status != EBidStatus.Expired)
                .ToListAsync();

        }


        //Get bids by transport request id and status
        public async Task<IEnumerable<Bid>> GetBidByTransportRequestAndStatusAsync(int transportRequestId, EBidStatus status)
        {
            return await _ctx.Bids
                .AsNoTracking()
                .Where(b => b.TransportRequestId == transportRequestId && b.Status == status)
                .ToListAsync();
        }

        //Cancel bid
        public async Task<bool> CancelBidAsync(int id)
        {
            var existingBid = await _ctx.Bids.FindAsync(id);
            if (existingBid == null || existingBid.Status != EBidStatus.Pendent)
            {
                return false;
            }


            existingBid.Status = EBidStatus.Canceled;
            await _ctx.SaveChangesAsync();
            return true;

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

