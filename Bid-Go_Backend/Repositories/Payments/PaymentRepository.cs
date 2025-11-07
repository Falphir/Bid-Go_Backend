using Bid_Go_Backend.Data;
using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Bid_Go_Backend.Repositories.Payments
{
    /// <summary>
    /// Repository responsible for payment persistence and queries.
    /// </summary>
    public class PaymentRepository : IPaymentRepository
    {
        private readonly BidGoDbContext _ctx;
        public PaymentRepository(BidGoDbContext ctx) => _ctx = ctx;

        /// <summary>
        /// Add a payment entity to the context (not saved until SaveChangesAsync).
        /// </summary>
        /// <param name="payment">Payment entity.</param>
        public async Task AddAsync(Payment payment)
            => await _ctx.Payments.AddAsync(payment);

        /// <summary>
        /// Persist pending changes to the datastore.
        /// </summary>
        public Task SaveChangesAsync() => _ctx.SaveChangesAsync();

        /// <summary>
        /// Retrieve a payment by id with tracking enabled for updates.
        /// </summary>
        /// <param name="paymentId">Payment identifier.</param>
        public Task<Payment?> GetByIdForUpdateAsync(int paymentId)
            => _ctx.Payments.AsTracking().FirstOrDefaultAsync(p => p.PaymentId == paymentId);

        /// <summary>
        /// List payments related to a user (either as company or driver), ordered by creation time descending.
        /// </summary>
        /// <param name="userId">User identifier.</param>
        public Task<List<Payment>> ListByUserAsync(int userId)
            => _ctx.Payments
                   .AsNoTracking()
                   .Where(p => p.CompanyId == userId || p.DriverId == userId)
                   .OrderByDescending(p => p.CreatedAt)
                   .ToListAsync();
    }
}
