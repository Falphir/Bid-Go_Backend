using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.Enums;
using Bid_Go_Backend.Data.Repositories.Interfaces;
using Bid_Go_Backend.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace Bid_Go_Backend.Data.Repositories.Chat
{
    public class ChatRepository : IChatRepository
    {
        private readonly BidGoDbContext _context;
        private readonly INotificationRepository _notificationRepo;
        public ChatRepository(BidGoDbContext context, INotificationRepository notificationRepo)
        {
            _context = context;
            _notificationRepo = notificationRepo;
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


        public async Task<IEnumerable<Message>> GetMessagesAsync(int chatId)
        {
            return await _context.Messages
                .Where(m => m.ChatId == chatId)
                .OrderBy(m => m.TimeStamp)
                .ToListAsync();
        }

        public async Task<Message> SendMessageAsync(Message message)
        {
            // Vai encontrar o chat
            var chat = await _context.Chats
                .FirstOrDefaultAsync(c => c.ChatId == message.ChatId);

            if (chat == null)
                throw new KeyNotFoundException("Chat não encontrado.");

            // Vai encontrar o TransportRequest associado
            var request = await _context.TransportRequests
                .FirstOrDefaultAsync(r => r.TransportRequestId == chat.TransportRequestId);

            if (request != null && request.Status == ERequestStatus.Canceled)
            {
                // Atualiza o status do chat para Archived
                chat.Status = EChatStatus.Archived;
                await _context.SaveChangesAsync();

                throw new InvalidOperationException("Não é possível enviar mensagens neste chat, pois o pedido foi cancelado.");
            }

            if (request != null && request.Status == ERequestStatus.Completed)
            {
                // Atualiza o status do chat para Archived
                chat.Status = EChatStatus.Archived;
                await _context.SaveChangesAsync();

                throw new InvalidOperationException("Não é possível enviar mensagens neste chat, pois o pedido foi concluido.");
            }

            // Verifica se o chat já está arquivado ou cancelado
            if (chat.Status == EChatStatus.Archived || chat.Status == EChatStatus.Canceled)
                throw new InvalidOperationException("Não é possível enviar mensagens neste chat.");

            // Adiciona a mensagem
            message.TimeStamp = DateTime.UtcNow;
            _context.Messages.Add(message);

            await _context.SaveChangesAsync();

            /*
            string notificationMessage;
            int receiverId;
            ENotificationType notificationType = ENotificationType.New_message;

            if (message.DriverId == 0 && message.CompanyId != 0)
            {
                // Mensagem enviada pela empresa → notifica o motorista
                var acceptedBid = await _context.Bids
                    .FirstOrDefaultAsync(b => b.TransportRequestId == chat.TransportRequestId && b.Status == EBidStatus.Accepted);

                if (acceptedBid != null)
                {
                    receiverId = acceptedBid.DriverId;
                    notificationMessage = $"Nova mensagem da empresa no pedido #{chat.TransportRequestId}.";

                    await _notificationRepo.CreateAsync(
                        receiverId,
                        notificationMessage,
                        notificationType,
                        null,
                        chat.TransportRequestId
                    );

                    await _notificationRepo.SendAsync(
                        receiverId,
                        notificationMessage,
                        notificationType
                    );
                }
            }
            else if (message.DriverId != 0)
            {
                // Mensagem enviada pelo motorista → notifica a empresa
                receiverId = request.CompanyId;
                notificationMessage = $"Nova mensagem do motorista no pedido #{chat.TransportRequestId}.";

                await _notificationRepo.CreateAsync(
                    receiverId,
                    notificationMessage,
                    notificationType,
                    null,
                    chat.TransportRequestId
                );

                await _notificationRepo.SendAsync(
                    receiverId,
                    notificationMessage,
                    notificationType
                );
            }
            */

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
