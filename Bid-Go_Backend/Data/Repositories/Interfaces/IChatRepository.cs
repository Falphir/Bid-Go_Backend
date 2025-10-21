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
        Task<Message> SendMessageAsync(Message message);
        Task<IEnumerable<Message>> GetMessagesAsync(int chatId);
    }
}
