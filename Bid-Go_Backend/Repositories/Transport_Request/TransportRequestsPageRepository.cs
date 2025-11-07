using Bid_Go_Backend.Data;
using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.Enums;
using Bid_Go_Backend.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bid_Go_Backend.Repositories.Transport_Request
{
    /// <summary>
    /// Repository for querying public transport requests displayed on the listing page.
    /// </summary>
    public class TransportRequestsPageRepository : ITransportRequestsPageRepository
    {
        private readonly BidGoDbContext _context;

        public TransportRequestsPageRepository(BidGoDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Get active transport requests with optional filters and ordering.
        /// </summary>
        /// <param name="origin">Optional origin substring to filter by.</param>
        /// <param name="destination">Optional destination substring to filter by.</param>
        /// <param name="deliveryDate">Optional exact delivery date to filter by.</param>
        /// <param name="priceOrder">Price ordering ("asc" or "desc"). Defaults to id ordering.</param>
        /// <returns>Enumerable of matching transport requests.</returns>
        public async Task<IEnumerable<TransportRequest>> GetActiveAsync(
            string? origin = null,
            string? destination = null,
            DateTime? deliveryDate = null,
            string? priceOrder = null
        )
        {
            var query = _context.TransportRequests
                .Where(tr => tr.Status == ERequestStatus.Active)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(origin))
            {
                query = query.Where(tr => tr.Origin.ToLower().Contains(origin.ToLower()));
            }


            if (!string.IsNullOrWhiteSpace(destination))
            {
                query = query.Where(tr => tr.Destination.ToLower().Contains(destination.ToLower()));
            }

            if (deliveryDate.HasValue)
            {
                query = query.Where(tr => tr.DeliveryDate.Date == deliveryDate.Value.Date);
            }

            query = priceOrder?.ToLower() switch
            {
                "asc" => query.OrderBy(tr => tr.MaxPrice),
                "desc" => query.OrderByDescending(tr => tr.MaxPrice),
                _ => query.OrderBy(tr => tr.TransportRequestId)
            };


            return await query.ToListAsync();
        }

        /// <summary>
        /// Get a single active transport request by identifier.
        /// </summary>
        /// <param name="id">Transport request identifier.</param>
        /// <returns>Transport request or null when not active or not found.</returns>
        public async Task<TransportRequest?> GetByIdAsync(int id)
        {
            return await _context.TransportRequests
                .Where(tr => tr.Status == ERequestStatus.Active)
                .FirstOrDefaultAsync(tr => tr.TransportRequestId == id);
        }
    }
}