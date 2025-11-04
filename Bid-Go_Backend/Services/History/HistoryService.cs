using Bid_Go_Backend.Data.Models.DTOs;
using Bid_Go_Backend.Data.Repositories.Interfaces;
using Bid_Go_Backend.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Bid_Go_Backend.Services.History
{
    public class HistoryService : IHistoryService
    {
        private readonly IHistoryRepository _repository;
        private readonly ILogger<HistoryService> _logger;

        public HistoryService(IHistoryRepository repository, ILogger<HistoryService> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<List<BidHistoryDTO>> GetDriverHistoryAsync(int driverId)
        {
            _logger.LogDebug("Fetching driver history for {DriverId}", driverId);
            return await _repository.GetDriverHistoryAsync(driverId);
        }

        public async Task<List<TransportHistoryDTO>> GetTransportHistoryAsync(int companyId)
        {
            _logger.LogDebug("Fetching transport history for {CompanyId}", companyId);
            return await _repository.GetTransportHistoryAsync(companyId);
        }
    }
}
