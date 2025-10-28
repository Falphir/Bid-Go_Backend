using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.Enums;
using Bid_Go_Backend.Data.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

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
                .Include(c => c.TransportRequest)
                .FirstOrDefaultAsync(c => c.TransportRequestId == requestId)
                ?? throw new KeyNotFoundException("Chat não encontrado.");
        }


        public async Task<IEnumerable<Message>> GetMessagesAsync(int chatId)
        {
            return await _context.Messages
                .Where(m => m.ChatId == chatId)
                .OrderBy(m => m.TimeStamp)
                .ToListAsync();
        }

        public async Task<Message> SendMessageAsync(Message message)
        {
            message.TimeStamp = DateTime.UtcNow;
            _context.Messages.Add(message);
            await _context.SaveChangesAsync();
            return message;
        }

        //Cria o chat a partir da bid aceite
        public async Task<Chats> CreateChatFromAcceptedBidAsync(int transportRequestId)
        {
            // Verifica se já existe chat para este pedido
            var existingChat = await _context.Chats
                .FirstOrDefaultAsync(c => c.TransportRequestId == transportRequestId);

            if (existingChat != null)
                return existingChat;

            // Procura a bid aceite
            var acceptedBid = await _context.Bids
                .FirstOrDefaultAsync(b => b.TransportRequestId == transportRequestId && b.Status == EBidStatus.Accepted);

            if (acceptedBid == null)
                throw new InvalidOperationException("Nenhuma bid aceite encontrada para este pedido.");

            // Verifica se o TransportRequest existe
            var transportRequest = await _context.TransportRequests
                .FirstOrDefaultAsync(t => t.TransportRequestId == transportRequestId);

            if (transportRequest == null)
                throw new KeyNotFoundException("Pedido de transporte não encontrado.");

            // Cria o chat
            var chat = new Chats
            {
                Status = EChatStatus.Active,
                TransportRequestId = transportRequestId
            };

            _context.Chats.Add(chat);
            await _context.SaveChangesAsync();

            return chat;
        }
    }
}
