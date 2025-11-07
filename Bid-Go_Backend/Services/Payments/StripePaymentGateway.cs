using Bid_Go_Backend.Services.Interfaces;
using Stripe;


namespace Bid_Go_Backend.Services.Payments
{
    /// <summary>
    /// Stripe-based implementation of IPaymentGateway using Stripe.net SDK.
    /// </summary>
    public class StripePaymentGateway : IPaymentGateway
    {
        private readonly ChargeService _chargeService = new();

        /// <summary>
        /// Create a Stripe charge and return a normalized result.
        /// </summary>
        /// <param name="amountCents">Amount in cents.</param>
        /// <param name="currency">Currency code (e.g., "eur").</param>
        /// <param name="sourceToken">Stripe token (e.g., from client-side Stripe.js).</param>
        /// <param name="description">Charge description.</param>
        /// <param name="metadata">Additional metadata for the charge.</param>
        public async Task<ChargeResult> ChargeAsync(long amountCents, string currency, string sourceToken, string description, IDictionary<string, string> metadata)
        {
            try
            {
                var charge = await _chargeService.CreateAsync(new ChargeCreateOptions
                {
                    Amount = amountCents,
                    Currency = currency,
                    Source = sourceToken,
                    Description = description,
                    Metadata = metadata?.ToDictionary(k => k.Key, v => v.Value)
                });

                return charge.Status == "succeeded"
                    ? new ChargeResult(true, null)
                    : new ChargeResult(false, charge.FailureMessage ?? "Charge failed.");
            }
            catch (StripeException ex)
            {
                return new ChargeResult(false, ex.Message);
            }
        }
    }
}
