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


        //Cancel bid
        public async Task<bool> CancelBidAsync(int id)
        {
            var existingBid = await _ctx.Bids.FindAsync(id);
            if (existingBid == null)
            {
                return false;
            }
            existingBid.Status = EBidStatus.Canceled;
            await _ctx.SaveChangesAsync();
            return true;

        }
      
    }
}