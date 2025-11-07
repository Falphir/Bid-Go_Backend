using Bid_Go_Backend.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bid_Go_Backend.Repositories.Interfaces
{
    public interface IAuthorizationRepository
    {
        Task<TransportRequest?> GetTransportRequestAsync(int transportRequestId);
        Task<Bid?> GetBidAsync(int bidId);
        Task<Payment?> GetPaymentAsync(int paymentId);
        Task<Chats?> GetChatWithRelationsAsync(int chatId);
        Task<TransportRequest?> GetTransportRequestWithSelectedBidAsync(int id);
    }
}
