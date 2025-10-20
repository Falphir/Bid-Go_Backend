using Bid_Go_Backend.Data;
using Microsoft.EntityFrameworkCore;
using Bid_Go_Backend.Repositories.Interface;
using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.Enums;


namespace Bid_Go_Backend.Repositories.BidRepo
{
    public class BidsCRUD : IBidCRUD
    {
        private readonly BidGoDbContext _ctx;

        public BidsCRUD(BidGoDbContext ctx)
        {
            _ctx = ctx;
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
        public async Task<bool> AcceptBidAsync(int id)
        {
            var existingBid = await _ctx.Bids
                .Include(b => b.TransportRequest)
                .FirstOrDefaultAsync(b => b.BidId == id);

            if (existingBid == null || existingBid.Status != EBidStatus.Pendent)
                return false;

            //Verify if the bid is alyready accepted

            bool hasAcceptedBid = await _ctx.Bids
                .AnyAsync(b => b.TransportRequestId == existingBid.TransportRequestId && b.Status == EBidStatus.Accepted);

            if (hasAcceptedBid)
                return false;


            existingBid.Status = EBidStatus.Accepted;


            //Reject other bids for the same transport request

            var otherBids = await _ctx.Bids
                .Where(b=> b.TransportRequestId == existingBid.TransportRequestId && b.BidId != id && b.Status == EBidStatus.Pendent)
                .ToListAsync();

            foreach (var bid in otherBids)
                bid.Status = EBidStatus.Rejected;


            if (existingBid.TransportRequest != null)
                existingBid.TransportRequest.Status = ERequestStatus.Pending;

            await _ctx.SaveChangesAsync();

            return true;

        }

    }
}