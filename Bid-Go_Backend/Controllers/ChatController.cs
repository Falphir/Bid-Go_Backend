using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.DTOs;
using Bid_Go_Backend.Services;
using Bid_Go_Backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using IAuthorizationService = Bid_Go_Backend.Services.Interfaces.IAuthorizationService;

namespace Bid_Go_Backend.Controllers
{
    [ApiController]
    [Route("api/chats")]
    [Authorize]
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chatService;
        private readonly IAuthorizationService _authorizationService;

        public ChatController(IChatService chatService, IAuthorizationService authorizationService)
        {
            _chatService = chatService;
            _authorizationService = authorizationService;
        }

        [HttpGet("{requestId}")]
        public async Task<IActionResult> GetChat(int requestId)
        {
            var result = await _chatService.GetChat(requestId, User);
            return StatusCode(result.StatusCode, result.Body);
        }

        [Authorize]
        [HttpPost("{chatId}/messages")]
        public async Task<IActionResult> SendMessage(int chatId, [FromBody] MessageSentDTO dto)
        {

            var userId = int.Parse(User.FindFirst("userId")?.Value);

            var ownsChat = await _authorizationService.UserOwnsChatAsync(userId, chatId);
            if (!ownsChat)
                return Forbid();


            var result = await _chatService.SendMessage(chatId, dto, User);
            return StatusCode(result.StatusCode, result.Body);
        }

        [Authorize]
        [HttpGet("{chatId}/get_messages")]
        public async Task<IActionResult> GetMessages(int chatId)
        {
            var userId = int.Parse(User.FindFirst("userId")?.Value);

            var ownsChat = await _authorizationService.UserOwnsChatAsync(userId, chatId);
            if (!ownsChat)
                return Forbid();


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
