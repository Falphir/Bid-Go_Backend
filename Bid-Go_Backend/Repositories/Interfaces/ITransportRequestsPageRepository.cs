using Bid_Go_Backend.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bid_Go_Backend.Repositories.Interfaces
{
    public interface ITransportRequestsPageRepository
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