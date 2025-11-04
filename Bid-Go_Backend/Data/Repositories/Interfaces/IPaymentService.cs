// Services/Payments/PaymentService.cs

// Services/Payments/PaymentService.cs
using Bid_Go_Backend.Data.Models.DTOs;

namespace Bid_Go_Backend.Data.Repositories.Interfaces
{
    public interface IPaymentService
    {
        Task<List<PaymentResultDTO>> GetPaymentsByUserAsync(int userId);
        Task<PaymentResultDTO> ProcessPaymentAsync(CreatePaymentRequestDTO request);
        Task<(bool Ok, string? Error, PaymentResultDTO? Result)> RetryPaymentAsync(int paymentId, string stripeToken);
    }
}