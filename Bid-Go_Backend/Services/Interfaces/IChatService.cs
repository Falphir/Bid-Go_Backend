using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bid_Go_Backend.Data.Models.DTOs;
using System.Security.Claims;

namespace Bid_Go_Backend.Services.Interfaces
{
    public interface IChatService
    {
        Task<(int StatusCode, object Body)> GetChat(int requestId, ClaimsPrincipal user);
        Task<(int StatusCode, object Body)> GetMessages(int chatId, ClaimsPrincipal user);
        Task<(int StatusCode, object Body)> SendMessage(int chatId, MessageDTO dto, ClaimsPrincipal user);
        Task<(int StatusCode, object Body)> CreateChatFromAcceptedBid(int transportRequestId);
    }
}
