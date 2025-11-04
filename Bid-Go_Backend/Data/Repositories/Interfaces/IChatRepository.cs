using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.DTOs;
using Bid_Go_Backend.Data.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bid_Go_Backend.Data.Repositories.Interfaces
{
    public interface IChatRepository
    {
        Task<Chats> GetChatByRequestIdAsync(int requestId);
        Task<Chats> GetChatByIdAsync(int requestId); 
        Task<IEnumerable<ChatMessageDTO>> GetMessagesAsync(int chatId);
        Task<Chats> CreateChatFromAcceptedBidAsync(int transportRequestId);

        Task<Message> AddMessageAsync(Message message);

        Task UpdateChatStatusAsync(Chats chat, EChatStatus status);
    }
}
