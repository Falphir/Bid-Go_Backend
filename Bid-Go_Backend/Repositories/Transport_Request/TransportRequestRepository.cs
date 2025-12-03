using Bid_Go_Backend.Data;
using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.Enums;
using Bid_Go_Backend.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Bid_Go_Backend.Repositories.Transport_Request
{
    /// <summary>
    /// Repository for managing persistence and retrieval of transport requests.
    /// </summary>
    public class TransportRequestRepository : ITransportRequestRepository
    {
        private readonly BidGoDbContext _context;

        public TransportRequestRepository(BidGoDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Create a new transport request and persist it.
        /// </summary>
        /// <param name="request">Transport request entity to create.</param>
        /// <returns>The created entity with keys populated.</returns>
        public async Task<TransportRequest> CreateAsync(TransportRequest request)
        {
            _context.TransportRequests.Add(request);
            await _context.SaveChangesAsync();
            return request;
        }

        /// <summary>
        /// Get a transport request by identifier.
        /// </summary>
        /// <param name="id">Transport request identifier.</param>
        /// <returns>The transport request or null when not found.</returns>
        public async Task<TransportRequest?> GetByIdAsync(int id)
        {
            return await _context.TransportRequests.FindAsync(id);
        }

        /// <summary>
        /// Get a transport request including its related bids.
        /// </summary>
        /// <param name="id">Transport request identifier.</param>
        /// <returns>The transport request with bids or null when not found.</returns>
        public async Task<TransportRequest?> GetRequestWithBidsByIdAsync(int id)
        {
            return await _context.TransportRequests
                  .Include(r => r.Bids)
                  .FirstOrDefaultAsync(r => r.TransportRequestId == id);
        }

        /// <summary>
        /// List all transport requests created by a given company ordered by pickup date.
        /// </summary>
        /// <param name="companyId">Company identifier.</param>
        /// <returns>List of transport requests.</returns>
        public async Task<List<TransportRequest>> GetAllByCompanyAsync(int companyId)
        {
            return await _context.TransportRequests
                .Where(r => r.CompanyId == companyId)
                .OrderBy(r => r.PickupDate)
                .ToListAsync();
        }

        /// <summary>
        /// Update a transport request and persist changes.
        /// </summary>
        /// <param name="id">Identifier of the transport request to update.</param>
        /// <param name="request">Modified transport request entity.</param>
        /// <returns>The updated entity.</returns>
        public async Task<TransportRequest> UpdateAsync(int id, TransportRequest request)
        {
 
            _context.TransportRequests.Update(request);
            await _context.SaveChangesAsync();
            return request;
        }


        /// <summary>
        /// Delete a transport request by identifier.
        /// </summary>
        /// <param name="id">Transport request identifier.</param>
        /// <returns>True when the entity existed and was removed; otherwise false.</returns>
        public async Task<bool> DeleteAsync(int id)
        {
            var alvo = await _context.TransportRequests
            .FirstOrDefaultAsync(r => r.TransportRequestId == id);

            if (alvo == null)
            {
                return false;
            }


            _context.TransportRequests.Remove(alvo);
            await _context.SaveChangesAsync();
            return true;
        }

    }
}
