using Bid_Go_Backend.Data.Models.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bid_Go_Backend.Data.Repositories.Interfaces
{
    public interface IHistoryRepository
    {
        Task<List<BidHistoryDTO>> GetDriverHistoryAsync(int driverId);

        //Task<List<TransportHistoryDTO>> GetTransportHistoryAsync(int companyId);
    }
}
