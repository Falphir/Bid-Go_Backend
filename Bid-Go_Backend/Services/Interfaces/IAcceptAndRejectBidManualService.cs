using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bid_Go_Backend.Services.Interfaces
{
    public interface IAcceptAndRejectBidManualService
    {

        Task<Bid?> GetBidByIdAsync(int id);
        Task<List<Bid>> GetBidsByTransportRequestAsync(int transportRequestId);
        Task<List<Bid>> GetBidsByTransportRequestAndStatusAsync(int transportRequestId, EBidStatus status);
        Task AcceptBidAsync(int id);
        Task RejectBidAsync(int id);
    }
}

