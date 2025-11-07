using Bid_Go_Backend.Data;
using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace Bid_Go_Backend.Repositories.Authorization
{
    /// <summary>
    /// Repository that provides read-only authorization related queries.
    /// </summary>
    /// <remarks>
    /// Encapsulates low-level EF Core queries used to verify ownership and related authorization checks.
    /// Business rules stay in services; only data retrieval lives here.
    /// </remarks>
    public class AuthorizationRepository : IAuthorizationRepository
    {
        private readonly BidGoDbContext _context;

        public AuthorizationRepository(BidGoDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Get a transport request by id (no tracking) for ownership validation.
        /// </summary>
        /// <param name="transportRequestId">Transport request identifier.</param>
        /// <returns>TransportRequest or null if not found.</returns>
        public async Task<TransportRequest?> GetTransportRequestAsync(int transportRequestId) =>
            await _context.TransportRequests
                .AsNoTracking()
                .FirstOrDefaultAsync(tr => tr.TransportRequestId == transportRequestId);

        /// <summary>
        /// Get a bid by id (no tracking) for ownership validation.
        /// </summary>
        /// <param name="bidId">Bid identifier.</param>
        /// <returns>Bid or null if not found.</returns>
        public async Task<Bid?> GetBidAsync(int bidId) =>
            await _context.Bids
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.BidId == bidId);

        /// <summary>
        /// Get a payment including its related transport request.
        /// </summary>
        /// <param name="paymentId">Payment identifier.</param>
        /// <returns>Payment or null if not found.</returns>
        public async Task<Payment?> GetPaymentAsync(int paymentId) =>
            await _context.Payments
                .Include(p => p.TransportRequest)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.PaymentId == paymentId);

        /// <summary>
        /// Get a chat including its transport request and selected bid.
        /// </summary>
        /// <param name="chatId">Chat identifier.</param>
        /// <returns>Chats entity or null if not found.</returns>
        public async Task<Chats?> GetChatWithRelationsAsync(int chatId) =>
            await _context.Chats
                .Include(c => c.TransportRequest)
                    .ThenInclude(tr => tr.SelectedBid)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.ChatId == chatId);

        /// <summary>
        /// Get a transport request including its selected bid.
        /// </summary>
        /// <param name="id">Transport request identifier.</param>
        /// <returns>TransportRequest or null if not found.</returns>
        public async Task<TransportRequest?> GetTransportRequestWithSelectedBidAsync(int id)
        {
            return await _context.TransportRequests
                .Include(tr => tr.SelectedBid)
                .AsNoTracking()
                .FirstOrDefaultAsync(tr => tr.TransportRequestId == id);
        }
    }
}
