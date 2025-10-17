using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.Enums;
using Bid_Go_Backend.Data.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bid_Go_Backend.Data.Repositories.Transport_Request
{
    public class TransportRequestsPageRepository : ITransportRequestsPageRepository
    {
        private readonly BidGoDbContext _context;

        public TransportRequestsPageRepository(BidGoDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<TransportRequest>> GetActiveAsync(
            string? origin = null,
            string? destination = null,
            DateTime? deliveryDate = null
        )
        {
            var query = _context.TransportRequests
                .Include(tr => tr.Company)
                .Include(tr => tr.Payment)
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


            return await query.ToListAsync();
        }

        public async Task<TransportRequest?> GetByIdAsync(int id)
        {
            return await _context.TransportRequests
                .Include(tr => tr.Company)
                .Include(tr => tr.Payment)
                .FirstOrDefaultAsync(tr => tr.TransportRequestId == id);
        }
    }
}