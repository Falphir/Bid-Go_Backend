using Bid_Go_Backend.Data;
using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.DTOs;
using Bid_Go_Backend.Data.Models.Enums;
using Bid_Go_Backend.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Bid_Go_Backend.Repositories.Chat
{
    public class ChatRepository : IChatRepository
    {
        private readonly BidGoDbContext _context;
        public ChatRepository(BidGoDbContext context, INotificationRepository notificationRepo)
        {
            _context = context;

        }

        public async Task<Chats> GetChatByRequestIdAsync(int requestId)
        {
            return await _context.Chats
                .Include(c => c.Messages)
                .Include(c => c.TransportRequest)
                .FirstOrDefaultAsync(c => c.TransportRequestId == requestId);
        }


        public async Task<Chats> GetChatByIdAsync(int chatId)
        {
            return await _context.Chats
                .Include(c => c.TransportRequest)
                .FirstOrDefaultAsync(c => c.ChatId == chatId);
        }


        public async Task<IEnumerable<ChatMessageDTO>> GetMessagesAsync(int chatId)
        {
            return await _context.Messages
                .Where(m => m.ChatId == chatId)
                .Select(m => new ChatMessageDTO
                {
                    Id = m.Id,
                    Context = m.Context,
                    TimeStamp = m.TimeStamp,
                    ChatId = m.ChatId,
                    DriverId = m.DriverId,
                    CompanyId = m.CompanyId
                })
                .OrderBy(m => m.TimeStamp)
                .ToListAsync();
        }
        public async Task<Message> AddMessageAsync(Message message)
        {
            _context.Messages.Add(message);
            await _context.SaveChangesAsync();
            return message;
        }

        public async Task UpdateChatStatusAsync(Chats chat, EChatStatus status)
        {
            chat.Status = status;
            await _context.SaveChangesAsync();
        }



        public async Task<Chats?> GetChatByTransportRequestIdAsync(int transportRequestId)
        {
            return await _context.Chats
                .FirstOrDefaultAsync(c => c.TransportRequestId == transportRequestId);
        }

        public async Task<Bid?> GetAcceptedBidByRequestIdAsync(int transportRequestId)
        {
            return await _context.Bids
                .FirstOrDefaultAsync(b => b.TransportRequestId == transportRequestId && b.Status == EBidStatus.Accepted);
        }

        public async Task<TransportRequest?> GetTransportRequestByIdAsync(int transportRequestId)
        {
            return await _context.TransportRequests
                .FirstOrDefaultAsync(t => t.TransportRequestId == transportRequestId);
        }

        public async Task<Chats> AddChatAsync(Chats chat)
        {
            _context.Chats.Add(chat);
            await _context.SaveChangesAsync();
            return chat;
        }

        public async Task ArchiveChatByTransportRequestIdAsync(int transportRequestId)
        {
            var chat = await _context.Chats
                .FirstOrDefaultAsync(c => c.TransportRequestId == transportRequestId);

            if (chat == null)
                throw new KeyNotFoundException("Chat não encontrado.");

            chat.Status = EChatStatus.Archived;
            await _context.SaveChangesAsync();
        }

    }
}