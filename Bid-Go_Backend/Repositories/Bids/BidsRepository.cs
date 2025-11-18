using Bid_Go_Backend.Data;
using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.Enums;
using Bid_Go_Backend.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;


namespace Bid_Go_Backend.Repositories.Bids
{
    /// <summary>
    /// Data access for Bid entities.
    /// </summary>
    public class BidsRepository : IBidsRepository
    {
        private readonly BidGoDbContext _ctx;
        public BidsRepository(BidGoDbContext ctx) => _ctx = ctx;

        /// <summary>
        /// Get a bid by id without tracking to avoid unnecessary change tracking for read operations.
        /// </summary>
        public async Task<Bid?> GetByIdAsync(int id) =>
            await _ctx.Bids.AsNoTracking().FirstOrDefaultAsync(b => b.BidId == id);

        /// <summary>
        /// Create a new bid and persist changes.
        /// </summary>
        public async Task<Bid> CreateAsync(Bid bid)
        {
            _ctx.Bids.Add(bid);
            await _ctx.SaveChangesAsync();
            return bid;
        }

        /// <summary>
        /// Update an existing bid and persist changes.
        /// </summary>
        public async Task<Bid> UpdateAsync(Bid bid)
        {
            _ctx.Bids.Update(bid);
            await _ctx.SaveChangesAsync();
            return bid;
        }

        /// <summary>
        /// List bids for a transport request.
        /// </summary>
        public async Task<List<Bid>> GetByTransportRequestAsync(int transportRequestId) =>
            await _ctx.Bids.AsNoTracking()
                .Where(b => b.TransportRequestId == transportRequestId)
                .ToListAsync();

        /// <summary>
        /// List bids for a transport request filtered by status.
        /// </summary>
        public async Task<IEnumerable<Bid>> GetByTransportRequestAndStatusAsync(int transportRequestId, EBidStatus status) =>
            await _ctx.Bids.AsNoTracking()
                .Where(b => b.TransportRequestId == transportRequestId && b.Status == status)
                .ToListAsync();

        /// <summary>
        /// Get active (pending) bids for a transport request with ordering options.
        /// </summary>
        /// <remarks>
        /// Includes the Driver navigation property to return driver info together with bids.
        /// </remarks>
        public async Task<List<Bid>> GetActiveBidsAsync(int transportRequestId, string? orderBy = "value", bool descending = false)
        {
            var query = _ctx.Bids
      .Include(b => b.Driver)
          .ThenInclude(d => d.ReviewsDriver) 
      .AsNoTracking()
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


        public async Task<List<Bid>> GetBidsByDriverId(int driverId)
        {
            var query = _ctx.Bids
             .Include(b => b.TransportRequest)
             .AsNoTracking()
             .Where(b => b.DriverId == driverId);

            return await query.ToListAsync();
        }
    }
}

