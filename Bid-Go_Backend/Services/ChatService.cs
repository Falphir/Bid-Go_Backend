using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.DTOs;
using Bid_Go_Backend.Data.Models.Enums;
using Bid_Go_Backend.Data.Repositories.Interfaces;
using Bid_Go_Backend.Services.Interfaces;
using System.Security.Claims;

namespace Bid_Go_Backend.Services
{
    public class ChatService : IChatService
    {
        private readonly IChatRepository _chatRepository;
        private readonly ITransportRequestRepository _requestRepository;

        public ChatService(IChatRepository chatRepository, ITransportRequestRepository requestRepository)
        {
            _chatRepository = chatRepository;
            _requestRepository = requestRepository;
        }

        public async Task<(int StatusCode, object Body)> GetChatAsync(int requestId, ClaimsPrincipal user)
        {
            var chat = await _chatRepository.GetChatByRequestIdAsync(requestId);
            if (chat == null)
                return (404, new { message = "Chat não encontrado." });

            if (!await UserHasAccessToChatAsync(user, chat.ChatId))
                return (403, new { message = "Acesso negado." });

            var dto = new ChatDTO
            {
                ChatId = chat.ChatId,
                Status = chat.Status,
                TransportRequestId = chat.TransportRequestId,
                Messages = chat.Messages.Select(m => new MessageDTO
                {
                    Context = m.Context,
                    DriverId = m.DriverId,
                    CompanyId = m.CompanyId,
                    TimeStamp = m.TimeStamp
                }).ToList()
            };

            return (200, dto);
        }

        public async Task<(int StatusCode, object Body)> GetMessagesAsync(int chatId, ClaimsPrincipal user)
        {
            if (!await UserHasAccessToChatAsync(user, chatId))
                return (403, new { message = "Acesso negado." });

            var messages = await _chatRepository.GetMessagesAsync(chatId);

            if (!messages.Any())
                return (404, new { message = "Nenhuma mensagem encontrada para este chat." });

            return (200, messages);
        }

        public async Task<(int StatusCode, object Body)> SendMessageAsync(int chatId, MessageDTO dto, ClaimsPrincipal user)
        {
            try
            {
                if (!await UserHasAccessToChatAsync(user, chatId))
                    return (403, new { message = "Acesso negado." });

                var chat = await _chatRepository.GetChatByIdAsync(chatId);
                var request = await _requestRepository.GetByIdAsync(chat.TransportRequestId);

                var userId = int.Parse(user.FindFirst("userId")!.Value);
                var role = user.FindFirst("userType")!.Value;

                int driverId = 0, companyId = 0;

                if (role == "Driver")
                {
                    driverId = userId;
                    companyId = request.CompanyId;
                }
                else if (role == "Company")
                {
                    companyId = userId;
                    var acceptedBid = request.Bids?.FirstOrDefault(b => b.Status == EBidStatus.Accepted);
                    if (acceptedBid == null)
                        return (400, new { message = "Nenhuma bid aceite encontrada." });

                    driverId = acceptedBid.DriverId;
                }

                var message = new Message
                {
                    ChatId = chatId,
                    Context = dto.Context,
                    DriverId = driverId,
                    CompanyId = companyId
                };

                var result = await _chatRepository.SendMessageAsync(message);

                var messageDto = new MessageDTO
                {
                    Context = result.Context,
                    TimeStamp = result.TimeStamp,
                    DriverId = result.DriverId,
                    CompanyId = result.CompanyId
                };

                return (200, messageDto);
            }
            catch (InvalidOperationException ex)
            {
                return (400, new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return (404, new { message = ex.Message });
            }
            catch
            {
                return (500, new { message = "Erro inesperado." });
            }
        }

        public async Task<(int StatusCode, object Body)> CreateChatFromAcceptedBidAsync(int transportRequestId)
        {
            try
            {
                var chat = await _chatRepository.CreateChatFromAcceptedBidAsync(transportRequestId);
                return (200, chat);
            }
            catch (Exception ex)
            {
                return (400, new { message = ex.Message });
            }
        }

        private async Task<bool> UserHasAccessToChatAsync(ClaimsPrincipal user, int chatId)
        {
            var chat = await _chatRepository.GetChatByIdAsync(chatId);
            if (chat == null) return false;

            var request = await _requestRepository.GetRequestWithBidsByIdAsync(chat.TransportRequestId);
            if (request == null) return false;

            var userIdClaim = user.FindFirst("userId")?.Value;
            var roleClaim = user.FindFirst("userType")?.Value;

            if (!int.TryParse(userIdClaim, out int userId) || string.IsNullOrEmpty(roleClaim))
                return false;

            var acceptedBid = request.Bids?.FirstOrDefault(b => b.Status == EBidStatus.Accepted);
            if (acceptedBid == null) return false;

            return (roleClaim == "Driver" && acceptedBid.DriverId == userId) ||
                   (roleClaim == "Company" && request.CompanyId == userId);
        }
    }
}
