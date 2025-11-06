using Bid_Go_Backend.Data;
using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Bid_Go_Backend.Repositories.Transport_Request
{
    public class TransportUpdateStatusRepository : ITransportUpdateStatus
    {
        private readonly BidGoDbContext _context;
        public TransportUpdateStatusRepository(BidGoDbContext context)
        {
            _context = context;
        }

        public async Task<TransportRequest?> GetTransportRequestWithBidsAsync(int id)
        {
            return await _context.TransportRequests
                .Include(r => r.Bids)
                .FirstOrDefaultAsync(r => r.TransportRequestId == id);
        }

        public async Task<User?> GetUserByIdAsync(int userId)
        {
            return await _context.Users.FindAsync(userId);
        }

        public void UpdateTransportRequest(TransportRequest request)
        {
            _context.TransportRequests.Update(request);
        }

        public void UpdateBids(IEnumerable<Bid> bids)
        {
            _context.Bids.UpdateRange(bids);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
