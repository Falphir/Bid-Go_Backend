    using Bid_Go_Backend.Data;
using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Bid_Go_Backend.Repositories.Transport_Request
{
    /// <summary>
    /// Repository for transport status updates. Provides data access for requests and related bids.
    /// </summary>
    public class TransportUpdateStatusRepository : ITransportUpdateStatus
    {
        private readonly BidGoDbContext _context;
        public TransportUpdateStatusRepository(BidGoDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Load a transport request including its bids.
        /// </summary>
        /// <param name="id">Transport request identifier.</param>
        /// <returns>Transport request or null when not found.</returns>
        public async Task<TransportRequest?> GetTransportRequestWithBidsAsync(int id)
        {
            return await _context.TransportRequests
                .Include(r => r.Bids)
                .FirstOrDefaultAsync(r => r.TransportRequestId == id);
        }

        /// <summary>
        /// Get a user by identifier.
        /// </summary>
        /// <param name="userId">User identifier.</param>
        /// <returns>User or null when not found.</returns>
        public async Task<User?> GetUserByIdAsync(int userId)
        {
            return await _context.Users.FindAsync(userId);
        }

        /// <summary>
        /// Mark a transport request entity as modified for persistence.
        /// </summary>
        /// <param name="request">Transport request entity tracked or detached.</param>
        public void UpdateTransportRequest(TransportRequest request)
        {
            _context.TransportRequests.Update(request);
        }

        /// <summary>
        /// Mark bid entities as modified for persistence.
        /// </summary>
        /// <param name="bids">Collection of bids to update.</param>
        public void UpdateBids(IEnumerable<Bid> bids)
        {
            _context.Bids.UpdateRange(bids);
        }

        /// <summary>
        /// Persist pending changes.
        /// </summary>
        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
