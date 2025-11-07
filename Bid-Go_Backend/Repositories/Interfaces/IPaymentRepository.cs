using Bid_Go_Backend.Data.Models;

namespace Bid_Go_Backend.Repositories.Interfaces
{
    public interface IPaymentRepository
    {
        Task AddAsync(Payment payment);
        Task<Payment?> GetByIdForUpdateAsync(int paymentId);
        Task<List<Payment>> ListByUserAsync(int userId);
        Task SaveChangesAsync();
    }
}