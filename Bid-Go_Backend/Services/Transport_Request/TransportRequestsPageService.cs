using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Repositories.Interfaces;
using Microsoft.Extensions.Logging;

namespace Bid_Go_Backend.Services.Transport_Request
{
    public interface ITransportRequestsPageService
    {
        Task<IEnumerable<TransportRequest>> GetActiveAsync(
            string? origin, string? destination, DateTime? deliveryDate, string? priceOrder);

        Task<TransportRequest?> GetByIdAsync(int id);
    }

    public class TransportRequestsPageService : ITransportRequestsPageService
    {
        private readonly ITransportRequestsPageRepository _repository;
        private readonly ILogger<TransportRequestsPageService> _logger;

        public TransportRequestsPageService(ITransportRequestsPageRepository repository, ILogger<TransportRequestsPageService> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<IEnumerable<TransportRequest>> GetActiveAsync(
            string? origin = null,
            string? destination = null,
            DateTime? deliveryDate = null,
            string? priceOrder = null)
        {
 
            if (deliveryDate.HasValue && deliveryDate.Value.Date < DateTime.UtcNow.Date)
                throw new ArgumentException("A data de entrega não pode ser anterior à data atual.");

            if (!string.IsNullOrEmpty(priceOrder) && priceOrder.ToLower() != "asc" && priceOrder.ToLower() != "desc")
                priceOrder = "asc";

            _logger.LogInformation($"[TransportService] Buscando pedidos ativos: Origem={origin}, Destino={destination}, Data={deliveryDate}, Ordem={priceOrder}");

            var results = await _repository.GetActiveAsync(origin, destination, deliveryDate, priceOrder);

            return results;
        }

        public async Task<TransportRequest?> GetByIdAsync(int id)
        {
            if (id <= 0)
                throw new ArgumentException("O ID deve ser maior que zero.");

            var request = await _repository.GetByIdAsync(id);

            if (request == null)
                _logger.LogWarning($"[TransportService] Pedido com ID {id} não encontrado.");

            return request;
        }
    }
}
