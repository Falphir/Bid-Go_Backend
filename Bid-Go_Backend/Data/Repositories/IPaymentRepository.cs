using Bid_Go_Backend.Data.Models.DTOs;

namespace Bid_Go_Backend.Data.Repositories
{
    public interface IPaymentRepository
    {
        Task<List<PaymentResultDTO>> GetPaymentsByUserAsync(int userId);
        Task<PaymentResultDTO> ProcessPaymentAsync(CreatePaymentRequestDTO request);
        Task<PaymentResultDTO> RetryPaymentAsync(int paymentId, string stripeToken);
    }
}