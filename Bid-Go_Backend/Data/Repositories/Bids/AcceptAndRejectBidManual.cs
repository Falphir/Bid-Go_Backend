using Bid_Go_Backend.Data;
using Microsoft.EntityFrameworkCore;
using Bid_Go_Backend.Repositories.Interface;
using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.Enums;
using Bid_Go_Backend.Data.Repositories.Interfaces;


namespace Bid_Go_Backend.Repositories.BidRepo
{
    public class AcceptAndRejectBidManual : IAcceptAndRejectBidManual
    {
        private readonly BidGoDbContext _ctx;
        private readonly INotificationRepository _notificationRepo;

        public AcceptAndRejectBidManual(BidGoDbContext ctx, INotificationRepository notificationRepo)
        {
            _ctx = ctx;
            _notificationRepo = notificationRepo;
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

        //Accept bid
        public async Task AcceptBidAsync(int id)
        {
            var existingBid = await _ctx.Bids
                .Include(b => b.TransportRequest)
                .FirstOrDefaultAsync(b => b.BidId == id);

            if (existingBid == null)
                throw new Exception("Bid not found.");

            if (existingBid.Status != EBidStatus.Pendent)
                throw new Exception("The bid is not pending and cannot be accepted");

            if (existingBid.TransportRequest == null)
                throw new Exception("Associated transport request not found");


            if (existingBid.TransportRequest.Status != ERequestStatus.Active)
                throw new Exception("The transport request is not active.");


            bool alreadyAccepted = await _ctx.Bids
                .AnyAsync(b => b.TransportRequestId == existingBid.TransportRequestId
                               && b.Status == EBidStatus.Accepted);

            if (alreadyAccepted)
                throw new Exception("There is already an accepted bid for this request");


            existingBid.Status = EBidStatus.Accepted;


            var otherBids = await _ctx.Bids
            .Where(b => b.TransportRequestId == existingBid.TransportRequestId
                     && b.BidId != id
                     && b.Status == EBidStatus.Pendent)
            .ToListAsync();

            foreach (var bid in otherBids)
                bid.Status = EBidStatus.Rejected;


            existingBid.TransportRequest.Status = ERequestStatus.Pending;

            await _ctx.SaveChangesAsync();

            await _notificationRepo.CreateAsync(
                existingBid.DriverId,
                "You bid was accepted.",
                ENotificationType.Accepted,
                existingBid.BidId,
                existingBid.TransportRequestId);


            await _notificationRepo.SendAsync(
                existingBid.DriverId,
                "Your bid was accepted",
                ENotificationType.Accepted);

            foreach (var bid in otherBids)
            {
                await _notificationRepo.CreateAsync(
                    bid.DriverId,
                    "A sua licitação foi recusada.",
                    ENotificationType.Rejected,
                    bid.BidId,
                    bid.TransportRequestId
                );

                await _notificationRepo.SendAsync(
                    bid.DriverId,
                    "A sua licitação foi recusada.",
                    ENotificationType.Rejected
                );
            }
        }
        


        public async Task RejectBidAsync(int id)
        {

            var existingBid = await _ctx.Bids
                .Include(b => b.TransportRequest)
                .FirstOrDefaultAsync(b => b.BidId == id);

            if (existingBid == null)
                throw new Exception("Bid not found.");


            if (existingBid.Status != EBidStatus.Pendent)
                throw new Exception("The bid is not pending and cannot be rejected");

            if (existingBid.TransportRequest == null)
                throw new Exception("Associated transport request not found");


            if (existingBid.TransportRequest.Status != ERequestStatus.Active)
                throw new Exception("The transport request is not active.");


            existingBid.Status = EBidStatus.Rejected;

            await _ctx.SaveChangesAsync();


            await _notificationRepo.CreateAsync(
                existingBid.DriverId,
                "A sua licitação foi recusada.",
                ENotificationType.Rejected,
                existingBid.BidId,
                existingBid.TransportRequestId
);

            await _notificationRepo.SendAsync(
                existingBid.DriverId,
                "A sua licitação foi recusada.",
                ENotificationType.Rejected
            );
        }
    }

    }
