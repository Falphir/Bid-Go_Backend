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
        Task<(int StatusCode, object Body)> GetChatAsync(int requestId, ClaimsPrincipal user);
        Task<(int StatusCode, object Body)> GetMessagesAsync(int chatId, ClaimsPrincipal user);
        Task<(int StatusCode, object Body)> SendMessageAsync(int chatId, MessageDTO dto, ClaimsPrincipal user);
        Task<(int StatusCode, object Body)> CreateChatFromAcceptedBidAsync(int transportRequestId);
    }
}
