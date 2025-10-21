using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bid_Go_Backend.Data.Repositories.Chat
{
    public class ChatRepository : IChatRepository
    {
        private readonly BidGoDbContext _context;

        public ChatRepository(BidGoDbContext context)
        {
            _context = context;
        }

        public async Task<Chats> GetChatByRequestIdAsync(int requestId)
        {
            return await _context.Chats
                .Include(c => c.Messages)
                .FirstOrDefaultAsync(c => c.TransportRequestId == requestId)
                ?? throw new KeyNotFoundException("Chat não encontrado.");
        }

        public async Task<Message> SendMessageAsync(Message message)
        {
            _context.Messages.Add(message);
            await _context.SaveChangesAsync();
            return message;
        }

        public async Task<IEnumerable<Message>> GetMessagesAsync(int chatId)
        {
            return await _context.Messages
                .Where(m => m.ChatId == chatId)
                .OrderBy(m => m.TimeStamp)
                .ToListAsync();
        }
    }
}
