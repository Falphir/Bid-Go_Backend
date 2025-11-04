using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bid_Go_Backend.Data.Models;

namespace Bid_Go_Backend.Services.Interfaces
{
    public interface ITransportRequestsPageService
    {
        Task<IEnumerable<TransportRequest>> GetActiveAsync(
           string? origin = null,
           string? destination = null,
           DateTime? deliveryDate = null,
           string? priceOrder = null
       );

        Task<TransportRequest?> GetByIdAsync(int id);
    }
}
