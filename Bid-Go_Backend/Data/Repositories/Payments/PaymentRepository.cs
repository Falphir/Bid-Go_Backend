using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.DTOs;
using Bid_Go_Backend.Data.Models.Enums;
using Bid_Go_Backend.Data.Repositories.Interfaces;
using Bid_Go_Backend.Repositories.Interface;
using Microsoft.EntityFrameworkCore;
using Stripe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bid_Go_Backend.Data.Repositories.Payments
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
