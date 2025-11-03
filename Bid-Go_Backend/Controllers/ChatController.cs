using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.DTOs;
using Bid_Go_Backend.Data.Models.Enums;
using Bid_Go_Backend.Data.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using System.Security.Claims;

namespace Bid_Go_Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ChatController : ControllerBase
    {
        private readonly IChatRepository _chatRepository;
        private readonly ITransportRequestRepository _requestRepository;

        public ChatController(IChatRepository chatRepository, ITransportRequestRepository requestRepository)
        {
            _chatRepository = chatRepository;
            _requestRepository = requestRepository;
        }

        private async Task<bool> UserHasAccessToChatAsync(int chatId)
        {
            // Obter o chat completo (com o pedido de transporte)
            var chat = await _chatRepository.GetChatByIdAsync(chatId);
            if (chat == null)
                return false;

            // Obter o pedido de transporte associado
            var request = await _requestRepository.GetByIdAsync(chat.TransportRequestId);
            if (request == null)
                return false;

            // Extrair ID e role do utilizador autenticado
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || string.IsNullOrEmpty(roleClaim))
                return false;

            int userId = int.Parse(userIdClaim);

            // Encontrar a Bid aceite 
            var acceptedBid = request.Bids?.FirstOrDefault(b => b.Status == EBidStatus.Accepted);
            if (acceptedBid == null)
                return false;

            // Verificar se o utilizador autenticado é o motorista da bid aceite ou a empresa do pedido
            bool isAuthorized =
                (roleClaim == "Driver" && acceptedBid.DriverId == userId) ||
                (roleClaim == "Company" && request.CompanyId == userId);

            return isAuthorized;
        }


        [HttpGet("{requestId}")]
        public async Task<IActionResult> GetChat(int requestId)
        {
            try
            {
                var chat = await _chatRepository.GetChatByRequestIdAsync(requestId);
                if (chat == null)
                    return NotFound(new { message = "Chat não encontrado." });

                if (!await UserHasAccessToChatAsync(chat.ChatId))
                    return StatusCode(403, new { message = "Acesso negado. Não pertence a este pedido de transporte." });



                var chatDto = new ChatDTO
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

                return Ok(chatDto);
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Ocorreu um erro inesperado." });
            }
        }

        [HttpPost("{chatId}/messages")]
        public async Task<IActionResult> SendMessage(int chatId, [FromBody] MessageDTO dto)
        {
            try
            {
                if (!await UserHasAccessToChatAsync(chatId))
                    return StatusCode(403, new { message = "Acesso negado. Não pertence a este pedido de transporte." });



                var message = new Message
                {
                    ChatId = chatId,
                    Context = dto.Context,
                    DriverId = dto.DriverId ?? 0,
                    CompanyId = dto.CompanyId ?? 0
                };

                var result = await _chatRepository.SendMessageAsync(message);

                var messageDto = new MessageDTO
                {
                    Context = result.Context,
                    TimeStamp = result.TimeStamp,
                    DriverId = result.DriverId,
                    CompanyId = result.CompanyId
                };

                return Ok(messageDto);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Ocorreu um erro inesperado." });
            }
        }

        [HttpGet("{chatId}/messages")]
        public async Task<IActionResult> GetMessages(int chatId)
        {
            if (!await UserHasAccessToChatAsync(chatId))
                return Forbid();

            var messages = await _chatRepository.GetMessagesAsync(chatId);
            return Ok(messages);
        }

        [HttpPost("create/{transportRequestId}")]
        public async Task<IActionResult> CreateChatFromAcceptedBid(int transportRequestId)
        {
            var chat = await _chatRepository.CreateChatFromAcceptedBidAsync(transportRequestId);
            return Ok(chat);
        }
    }
}
