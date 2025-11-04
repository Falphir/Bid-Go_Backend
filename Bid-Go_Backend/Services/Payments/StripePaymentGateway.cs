using Stripe;


namespace Bid_Go_Backend.Services.Payments
{
    public class StripePaymentGateway : IPaymentGateway
    {
        private readonly ChargeService _chargeService = new();

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
