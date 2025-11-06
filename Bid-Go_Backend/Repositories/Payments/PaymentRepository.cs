using Bid_Go_Backend.Data;
using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Bid_Go_Backend.Repositories.Payments
{
    public class PaymentRepository : IPaymentRepository
    {
        private readonly BidGoDbContext _ctx;
        public PaymentRepository(BidGoDbContext ctx) => _ctx = ctx;

        public async Task AddAsync(Payment payment)
            => await _ctx.Payments.AddAsync(payment);

        public Task SaveChangesAsync() => _ctx.SaveChangesAsync();

        public Task<Payment?> GetByIdForUpdateAsync(int paymentId)
            => _ctx.Payments.AsTracking().FirstOrDefaultAsync(p => p.PaymentId == paymentId);

        public Task<List<Payment>> ListByUserAsync(int userId)
            => _ctx.Payments
                   .AsNoTracking()
                   .Where(p => p.CompanyId == userId || p.DriverId == userId)
                   .OrderByDescending(p => p.CreatedAt)
                   .ToListAsync();
    }
}
