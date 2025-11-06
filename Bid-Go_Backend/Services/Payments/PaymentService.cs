using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.DTOs;
using Bid_Go_Backend.Data.Models.Enums;
using Bid_Go_Backend.Repositories.Interfaces;
using Bid_Go_Backend.Services.Interfaces;

namespace Bid_Go_Backend.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly IPaymentRepository _payments;
        private readonly IBidsService _bids;
        private readonly ITransportRequestRepository _transportReqs;
        private readonly INotificationService _notificationService;
        private readonly IPaymentGateway _gateway;

        public PaymentService(
            IPaymentRepository payments,
            IBidsService bids,
            ITransportRequestRepository transportReqs,
            INotificationService notificationService,
            IPaymentGateway gateway)
        {
            _payments = payments;
            _bids = bids;
            _transportReqs = transportReqs;
            _notificationService = notificationService;
            _gateway = gateway;
        }

        public async Task<PaymentResultDTO> ProcessPaymentAsync(CreatePaymentRequestDTO request)
        {
            //1) Buscar TR + Bid
            var tr = await _transportReqs.GetByIdAsync(request.TransportRequestId)
             ?? throw new InvalidOperationException("Transport request was not found.");

            if (tr.SelectedBidId is null)
                throw new InvalidOperationException("No selected bid for this transport request.");

            var bid = await _bids.GetBidByIdAsync(tr.SelectedBidId.Value)
                      ?? throw new InvalidOperationException("Selected bid no longer exists.");

            if (bid.TransportRequestId != tr.TransportRequestId)
                throw new InvalidOperationException("Selected bid does not belong to the given transport request.");

            //2) Cálculos
            var bidValue = bid.Value;
            var tax = Math.Round(bidValue *0.05m,2);
            var gross = bidValue;
            var netForDriver = bidValue - tax;

            //3) Criar pagamento pendente
            var payment = new Payment
            {
                CompanyId = tr.CompanyId,
                DriverId = bid.DriverId,
                TransportRequestId = tr.TransportRequestId,
                GrossValue = gross,
                Tax = tax,
                NetValue = netForDriver,
                PaymentMethod = EPaymentMethod.Stripe,
                PaymentStatus = EPaymentStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                DeadlineToPay = DateTime.UtcNow.AddDays(3)
            };

            await _payments.AddAsync(payment);
            await _payments.SaveChangesAsync();

            //4) Cobrança via gateway
            var charge = await _gateway.ChargeAsync(
                amountCents: (long)(payment.GrossValue *100),
                currency: "eur",
                sourceToken: request.StripeToken,
                description: $"Payment for transport request {payment.TransportRequestId}",
                metadata: new Dictionary<string, string> { ["paymentId"] = payment.PaymentId.ToString() }
            );

            if (charge.Success)
            {
                payment.PaymentStatus = EPaymentStatus.Confirmed;
                payment.CompletedAt = DateTime.UtcNow;
                payment.FailureReason = null;
                tr.Status = ERequestStatus.WaitingPickup;
                await _payments.SaveChangesAsync();

                await _notificationService.CreateAndSendAsync(
                    tr.CompanyId,
                    $"Payment for transport request #{payment.TransportRequestId} confirmed successfully.",
                    ENotificationType.Confirmed_Payment,
                    null,
                    payment.TransportRequestId
                );
            }
            else
            {
                payment.PaymentStatus = EPaymentStatus.Failed;
                payment.CompletedAt = null;
                payment.FailureReason = charge.ErrorMessage ?? "Stripe did not accept payment.";
                await _payments.SaveChangesAsync();
            }

            return ToDto(payment);
        }

        public async Task<List<PaymentResultDTO>> GetPaymentsByUserAsync(int userId)
        {
            var items = await _payments.ListByUserAsync(userId);
            return items.Select(ToDto).ToList();
        }

        public async Task<(bool Ok, string? Error, PaymentResultDTO? Result)> RetryPaymentAsync(int paymentId, string stripeToken)
        {
            var payment = await _payments.GetByIdForUpdateAsync(paymentId)
                          ?? throw new InvalidOperationException("Payment was not found.");

            if (payment.PaymentStatus == EPaymentStatus.Confirmed)
                return (false, "This payment has already been completed.", ToDto(payment));
            
            // Only enforce deadline for pending payments; allow retry on failed ones regardless of deadline
            if (payment.PaymentStatus == EPaymentStatus.Pending && payment.DeadlineToPay < DateTime.UtcNow)
            {
                payment.PaymentStatus = EPaymentStatus.Pending;
                payment.FailureReason = "The payment deadline has passed. Please create a new payment.";
                await _payments.SaveChangesAsync();
                return (false, "The payment deadline has passed. Please create a new payment.", ToDto(payment));
            }

            var tr = await _transportReqs.GetByIdAsync(payment.TransportRequestId)
             ?? throw new InvalidOperationException("Transport request was not found.");

            var charge = await _gateway.ChargeAsync(
                amountCents: (long)(payment.GrossValue *100),
                currency: "eur",
                sourceToken: stripeToken,
                description: $"Retry payment {payment.PaymentId}",
                metadata: new Dictionary<string, string> { ["paymentId"] = payment.PaymentId.ToString() }
            );

            if (charge.Success)
            {
                payment.PaymentStatus = EPaymentStatus.Confirmed;
                payment.CompletedAt = DateTime.UtcNow;
                payment.FailureReason = null;
                tr.Status = ERequestStatus.WaitingPickup;
                await _payments.SaveChangesAsync();

                await _notificationService.CreateAndSendAsync(
                    payment.CompanyId,
                    "Your payment was successfully completed after retry.",
                    ENotificationType.Confirmed_Payment,
                    null,
                    payment.TransportRequestId
                );

                return (true, null, ToDto(payment));
            }

            payment.PaymentStatus = EPaymentStatus.Failed;
            payment.CompletedAt = null;
            payment.FailureReason = charge.ErrorMessage ?? "Stripe did not accept the payment.";
            await _payments.SaveChangesAsync();

            return (false, payment.FailureReason, ToDto(payment));
        }

        private static PaymentResultDTO ToDto(Payment p) => new()
        {
            PaymentId = p.PaymentId,
            GrossValue = p.GrossValue,
            Tax = p.Tax,
            NetValue = p.NetValue,
            Status = p.PaymentStatus,
            FailureReason = p.FailureReason,
            CreatedAt = p.CreatedAt,
            CompletedAt = p.CompletedAt
        };
    }
}
