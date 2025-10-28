using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.DTOs;
using Bid_Go_Backend.Data.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Bid_Go_Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly IChatRepository _chatRepository;

        public ChatController(IChatRepository chatRepository)
        {
            _chatRepository = chatRepository;
        }

        [HttpGet("{requestId}")]
        public async Task<IActionResult> GetChat(int requestId)
        {
            var chat = await _chatRepository.GetChatByRequestIdAsync(requestId);
            return Ok(chat);
        }

        [HttpPost("{chatId}/messages")]
        public async Task<IActionResult> SendMessage(int chatId, [FromBody] MessageDTO dto)
        {
            var message = new Message
            {
                ChatId = chatId,
                Context = dto.Context,
                DriverId = dto.DriverId ?? 0,
                CompanyId = dto.CompanyId ?? 0
            };

            var result = await _chatRepository.SendMessageAsync(message);
            return Ok(result);
        }

        [HttpGet("{chatId}/messages")]
        public async Task<IActionResult> GetMessages(int chatId)
        {
            var messages = await _chatRepository.GetMessagesAsync(chatId);
            return Ok(messages);
        }

        //Cria o chat automaticamente quando há bid aceite
        [HttpPost("create/{transportRequestId}")]
        public async Task<IActionResult> CreateChatFromAcceptedBid(int transportRequestId)
        {
            var chat = await _chatRepository.CreateChatFromAcceptedBidAsync(transportRequestId);
            return Ok(chat);
        }
    }
}
