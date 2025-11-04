using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Repositories.Interfaces;
using Bid_Go_Backend.Services.Interfaces;

namespace Bid_Go_Backend.Services
{
    public class TransportRequestsPageService : ITransportRequestsPageService
    {
        private readonly ITransportRequestsPageRepository _repository;

        public TransportRequestsPageService(ITransportRequestsPageRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<TransportRequest>> GetActiveAsync(
            string? origin = null,
            string? destination = null,
            DateTime? deliveryDate = null,
            string? priceOrder = null
        )
        {
  
            return await _repository.GetActiveAsync(origin, destination, deliveryDate, priceOrder);
        }

        public async Task<TransportRequest?> GetByIdAsync(int id)
        {
            return await _repository.GetByIdAsync(id);
        }
    }
}
