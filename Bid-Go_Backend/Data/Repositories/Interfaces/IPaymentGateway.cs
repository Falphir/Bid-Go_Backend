
namespace Bid_Go_Backend.Services.Payments
{
    public interface IPaymentGateway
    {
        Task<ChargeResult> ChargeAsync(long amountCents, string currency, string sourceToken, string description, IDictionary<string, string> metadata);
    }
}