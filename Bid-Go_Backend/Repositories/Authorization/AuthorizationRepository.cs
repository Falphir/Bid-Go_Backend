using Bid_Go_Backend.Data;
using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace Bid_Go_Backend.Repositories.Authorization
{
    public class AuthorizationRepository : IAuthorizationRepository
    {
        private readonly BidGoDbContext _context;

        public AuthorizationRepository(BidGoDbContext context)
        {
            _context = context;
        }

        public async Task<TransportRequest?> GetTransportRequestAsync(int transportRequestId) =>
            await _context.TransportRequests
                .AsNoTracking()
                .FirstOrDefaultAsync(tr => tr.TransportRequestId == transportRequestId);

        public async Task<Bid?> GetBidAsync(int bidId) =>
            await _context.Bids
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.BidId == bidId);

        public async Task<Payment?> GetPaymentAsync(int paymentId) =>
            await _context.Payments
                .Include(p => p.TransportRequest)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.PaymentId == paymentId);

        public async Task<Chats?> GetChatWithRelationsAsync(int chatId) =>
            await _context.Chats
                .Include(c => c.TransportRequest)
                    .ThenInclude(tr => tr.SelectedBid)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.ChatId == chatId);

        public async Task<TransportRequest?> GetTransportRequestWithSelectedBidAsync(int id)
        {
            return await _context.TransportRequests
                .Include(tr => tr.SelectedBid) 
                .AsNoTracking()
                .FirstOrDefaultAsync(tr => tr.TransportRequestId == id);
        }
    }
}
