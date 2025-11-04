using Bid_Go_Backend.Data.Models.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Bid_Go_Backend.Services.Interfaces
{
    public interface IHistoryService
    {
        Task<List<BidHistoryDTO>> GetDriverHistoryAsync(int driverId);
        Task<List<TransportHistoryDTO>> GetTransportHistoryAsync(int companyId);
    }
}
