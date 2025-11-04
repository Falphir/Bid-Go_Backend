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
        private readonly INotificationRepository _notificationRepo;
        public PaymentRepository(BidGoDbContext ctx, INotificationRepository notificationRepo)
        {
            _ctx = ctx;
            _notificationRepo = notificationRepo;
        }

        public async Task<PaymentResultDTO> ProcessPaymentAsync(CreatePaymentRequestDTO request)
        {
            var bid = await _ctx.Bids
                .Include(b => b.TransportRequest)
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.BidId == request.BidId);

            if (bid == null)
                throw new InvalidOperationException("Bid was not found.");

            if (bid.TransportRequest == null)
                throw new InvalidOperationException("Transport request associated to this bid was not found.");

            var bidValue = bid.Value;
            var tax = Math.Round(bidValue * 0.05m, 2);
            var gross = bidValue;
            var netForDriver = bidValue - tax;

            var payment = new Payment
            {
                CompanyId = bid.TransportRequest.CompanyId,
                DriverId = bid.DriverId,
                TransportRequestId = bid.TransportRequestId,

                GrossValue = gross,
                Tax = tax,
                NetValue = netForDriver,

                PaymentMethod = EPaymentMethod.Stripe,
                PaymentStatus = EPaymentStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                DeadlineToPay = DateTime.UtcNow.AddDays(3)
            };

            _ctx.Payments.Add(payment);
            await _ctx.SaveChangesAsync();

            try
            {
                var chargeService = new Stripe.ChargeService();
                var charge = await chargeService.CreateAsync(new Stripe.ChargeCreateOptions
                {
                    Amount = (long)(payment.GrossValue * 100),
                    Currency = "eur",
                    Source = request.StripeToken,
                    Description = $"Payment for bid related to transport request {payment.TransportRequestId}",
                    Metadata = new Dictionary<string, string>
            {
                { "paymentId", payment.PaymentId.ToString() }
            }
                });

                if (charge.Status == "succeeded")
                {
                    payment.PaymentStatus = EPaymentStatus.Confirmed;
                    payment.CompletedAt = DateTime.UtcNow;
                    payment.FailureReason = null;

                    /*
                    await _notificationRepo.CreateAsync(
                                  payment.CompanyId,
                                  $"Payment for transport request #{payment.TransportRequestId} confirmed successfully.",
                                  ENotificationType.Confirmed_Payment,
                                  null,
                                  payment.TransportRequestId
                              );
                    await _notificationRepo.SendAsync(
                        payment.CompanyId,
                        $"Payment for transport request #{payment.TransportRequestId} confirmed successfully.",
                        ENotificationType.Confirmed_Payment
                    );
                    */
                }
            
                else
                {
                    payment.PaymentStatus = EPaymentStatus.Failed;
                    payment.FailureReason = charge.FailureMessage ?? "Stripe did not accept payment.";
                }
            }
            catch (Stripe.StripeException ex)
            {
                payment.PaymentStatus = EPaymentStatus.Failed;
                payment.FailureReason = ex.Message;
            }

            await _ctx.SaveChangesAsync();

            return new PaymentResultDTO
            {
                PaymentId = payment.PaymentId,
                GrossValue = payment.GrossValue,
                Tax = payment.Tax,
                NetValue = payment.NetValue,
                Status = payment.PaymentStatus,
                FailureReason = payment.FailureReason,
                CreatedAt = payment.CreatedAt,
                CompletedAt = payment.CompletedAt
            };
        }


        public async Task<List<PaymentResultDTO>> GetPaymentsByUserAsync(int userId)
        {
            var query = _ctx.Payments.AsNoTracking();

            var payments = await query
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return payments.Select(p => new PaymentResultDTO
            {
                PaymentId = p.PaymentId,
                GrossValue = p.GrossValue,
                Tax = p.Tax,
                NetValue = p.NetValue,
                Status = p.PaymentStatus,
                FailureReason = p.FailureReason,
                CreatedAt = p.CreatedAt,
                CompletedAt = p.CompletedAt
            }).ToList();
        }

        public async Task<PaymentResultDTO> RetryPaymentAsync(int paymentId, string stripeToken)
        {
            var payment = await _ctx.Payments
                .AsTracking()
                .FirstOrDefaultAsync(p => p.PaymentId == paymentId);

            if (payment == null)
                throw new InvalidOperationException("Payment was not found.");

            if (payment.PaymentStatus == EPaymentStatus.Confirmed)
                throw new InvalidOperationException("This payment has already been completed.");

            if (payment.DeadlineToPay < DateTime.UtcNow)
            {
                payment.PaymentStatus = EPaymentStatus.Pending;
                payment.FailureReason = "The payment deadline has passed. Please create a new payment.";
                await _ctx.SaveChangesAsync();
                throw new InvalidOperationException("The payment deadline has passed. Please create a new payment.");
            }

            try
            {
                var chargeService = new Stripe.ChargeService();
                var charge = await chargeService.CreateAsync(new Stripe.ChargeCreateOptions
                {
                    Amount = (long)(payment.GrossValue * 100),
                    Currency = "eur",
                    Source = stripeToken,
                    Description = $"Nova tentativa do pagamento {payment.PaymentId}",
                    Metadata = new Dictionary<string, string>
            {
                { "paymentId", payment.PaymentId.ToString() }
            }
                });

                if (charge.Status == "succeeded")
                {
                    payment.PaymentStatus = EPaymentStatus.Confirmed;
                    payment.CompletedAt = DateTime.UtcNow;
                    payment.FailureReason = null;

                    await _ctx.SaveChangesAsync();
                    /*

                    await _notificationRepo.CreateAsync(
                        payment.CompanyId,
                        "Your payment was successfully completed after retry.",
                        ENotificationType.Confirmed_Payment,
                        null,
                        payment.TransportRequestId
                    );

                    await _notificationRepo.SendAsync(
                        payment.CompanyId,
                        "Your payment was successfully completed after retry.",
                        ENotificationType.Confirmed_Payment
                    );
                    */
                }
                else
                {
                    payment.PaymentStatus = EPaymentStatus.Failed;
                    payment.CompletedAt = null;
                    payment.FailureReason = charge.FailureMessage ?? "Stripe não aceitou o pagamento.";
                }
            }
            catch (Stripe.StripeException ex)
            {
                payment.PaymentStatus = EPaymentStatus.Failed;
                payment.CompletedAt = null;
                payment.FailureReason = ex.Message;
                await _ctx.SaveChangesAsync();
            }


            return new PaymentResultDTO
            {
                PaymentId = payment.PaymentId,
                GrossValue = payment.GrossValue,
                Tax = payment.Tax,
                NetValue = payment.NetValue,
                Status = payment.PaymentStatus,
                FailureReason = payment.FailureReason,
                CreatedAt = payment.CreatedAt,
                CompletedAt = payment.CompletedAt
            };
        }

    }
}
