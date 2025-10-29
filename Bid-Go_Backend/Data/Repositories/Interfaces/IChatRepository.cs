using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bid_Go_Backend.Data.Models;

namespace Bid_Go_Backend.Data.Repositories.Interfaces
{
    public interface IChatRepository
    {
        Task<Chats> GetChatByRequestIdAsync(int requestId);
        Task<IEnumerable<Message>> GetMessagesAsync(int chatId);
        Task<Message> SendMessageAsync(Message message);
        Task<Chats> CreateChatFromAcceptedBidAsync(int transportRequestId);
        Task<Bid?> GetAcceptedBidAsync(int transportRequestId);
        Task<Chats?> GetChatByIdWithRequestAsync(int chatId);
        Task<Company?> GetCompanyByIdAsync(int companyId);
        Task<Driver?> GetDriverByIdAsync(int driverId);


    }
}
