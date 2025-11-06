using Bid_Go_Backend.Data;
using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.Enums;
using Bid_Go_Backend.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Bid_Go_Backend.Repositories.Transport_Request
{
    public class TransportRequestRepository : ITransportRequestRepository
    {
        private readonly BidGoDbContext _context;

        public TransportRequestRepository(BidGoDbContext context)
        {
            _context = context;
        }

        public async Task<TransportRequest> CreateAsync(TransportRequest request)
        {
            // Do not override status here; keep what the service decided (e.g., Draft)
            _context.TransportRequests.Add(request);
            await _context.SaveChangesAsync();
            return request;
        }

        public async Task<TransportRequest?> GetByIdAsync(int id)
        {
            return await _context.TransportRequests.FindAsync(id);
        }

        public async Task<TransportRequest?> GetRequestWithBidsByIdAsync(int id)
        {
            return await _context.TransportRequests
                  .Include(r => r.Bids)
                  .FirstOrDefaultAsync(r => r.TransportRequestId == id);
        }

        public async Task<List<TransportRequest>> GetAllByCompanyAsync(int companyId)
        {
            return await _context.TransportRequests
                .Where(r => r.CompanyId == companyId)
                .OrderBy(r => r.PickupDate)
                .ToListAsync();
        }

        public async Task<TransportRequest> UpdateAsync(int id, TransportRequest request)
        {
 
            _context.TransportRequests.Update(request);
            await _context.SaveChangesAsync();
            return request;
        }


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
