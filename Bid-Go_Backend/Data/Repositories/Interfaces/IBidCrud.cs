using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bid_Go_Backend.Repositories.Interface
{
    public interface IBidCRUD
    {
 
        Task<Bid?> GetBidByIdAsync(int id);
        Task<List<Bid>> GetBidByTransportRequestAsync(int transportRequestId);
        Task<IEnumerable<Bid>> GetBidByTransportRequestAndStatusAsync(int transportRequestId, EBidStatus status);
        Task AcceptBidAsync(int id);

        Task RejectBidAsync(int id);
    }
}