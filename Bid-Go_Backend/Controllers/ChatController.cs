using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.DTOs;
using Bid_Go_Backend.Data.Models.Enums;
using Bid_Go_Backend.Data.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
            var chat = await _chatRepository.GetChatByIdAsync(chatId);
            if (chat == null) return false;

            var request = await _requestRepository.GetRequestWithBidsByIdAsync(chat.TransportRequestId);
            if (request == null) return false;

            var userIdClaim = User.FindFirst("userId")?.Value;
            var roleClaim = User.FindFirst("userType")?.Value;

            if (!int.TryParse(userIdClaim, out int userId) || string.IsNullOrEmpty(roleClaim))
                return false;

            var acceptedBid = request.Bids?.FirstOrDefault(b => b.Status == EBidStatus.Accepted);
            if (acceptedBid == null) return false;

            return (roleClaim == "Driver" && acceptedBid.DriverId == userId) ||
                   (roleClaim == "Company" && request.CompanyId == userId);
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

                var chat = await _chatRepository.GetChatByIdAsync(chatId);
                var request = await _requestRepository.GetByIdAsync(chat.TransportRequestId);

                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
                var role = User.FindFirst(ClaimTypes.Role)!.Value;

                int driverId = 0;
                int companyId = 0;

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
                        return BadRequest(new { message = "Nenhuma bid aceite encontrada." });

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
                return StatusCode(403, new { message = "Acesso negado. Não pertence a este pedido de transporte." });

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
