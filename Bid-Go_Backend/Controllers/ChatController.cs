using Bid_Go_Backend.Data.Models.DTOs;
using Bid_Go_Backend.Services;
using Bid_Go_Backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bid_Go_Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chatService;

        public ChatController(IChatService chatService)
        {
            _chatService = chatService;
        }

        [HttpGet("{requestId}")]
        public async Task<IActionResult> GetChat(int requestId)
        {
            var result = await _chatService.GetChat(requestId, User);
            return StatusCode(result.StatusCode, result.Body);
        }

        [Authorize]
        [HttpPost("{chatId}/messages")]
        public async Task<IActionResult> SendMessage(int chatId, [FromBody] MessageDTO dto)
        {
            var result = await _chatService.SendMessage(chatId, dto, User);
            return StatusCode(result.StatusCode, result.Body);
        }

        [Authorize]
        [HttpGet("{chatId}/get_messages")]
        public async Task<IActionResult> GetMessages(int chatId)
        {
            var result = await _chatService.GetMessages(chatId, User);
            return StatusCode(result.StatusCode, result.Body);
        }

        [Authorize]
        [HttpPost("create/{transportRequestId}")]
        public async Task<IActionResult> CreateChatFromAcceptedBid(int transportRequestId)
        {
            var result = await _chatService.CreateChatFromAcceptedBid(transportRequestId);
            return StatusCode(result.StatusCode, result.Body);
        }
    }
}
