using Bid_Go_Backend.Data.Models.DTOs;
using Bid_Go_Backend.Repositories.Interfaces;
using Bid_Go_Backend.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Bid_Go_Backend.Services.History
{
    /// <summary>
    /// Service that exposes history retrieval operations for drivers and companies.
    /// </summary>
    public class HistoryService : IHistoryService
    {
        private readonly IHistoryRepository _repository;
        private readonly ILogger<HistoryService> _logger;

        public HistoryService(IHistoryRepository repository, ILogger<HistoryService> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        /// <summary>
        /// Get historical bid events for a driver.
        /// </summary>
        /// <param name="driverId">Driver identifier.</param>
        public async Task<List<BidHistoryDTO>> GetDriverHistoryAsync(int driverId)
        {
            _logger.LogDebug("Fetching driver history for {DriverId}", driverId);
            return await _repository.GetDriverHistoryAsync(driverId);
        }

        /// <summary>
        /// Get historical transport entries for a company.
        /// </summary>
        /// <param name="companyId">Company identifier.</param>
        public async Task<List<TransportHistoryDTO>> GetTransportHistoryAsync(int companyId)
        {
            _logger.LogDebug("Fetching transport history for {CompanyId}", companyId);
            return await _repository.GetTransportHistoryAsync(companyId);
        }
    }
}
