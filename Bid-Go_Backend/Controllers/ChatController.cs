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
            try
            {
                var chat = await _chatRepository.GetChatByRequestIdAsync(requestId);
                if (chat == null)
                    return NotFound(new { message = "Chat não encontrado." });

   
                var chatDto = new ChatDTO
                {
                    ChatId = chat.ChatId,
                    Status = chat.Status,
                    TransportRequestId = chat.TransportRequestId,
                    Messages = chat.Messages.Select(m => new MessageDTO
                    {
                        Context = m.Context, 
                        DriverId = m.DriverId,
                        CompanyId = m.CompanyId
                    }).ToList()
                };

                return Ok(chatDto);
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

        [HttpPost("{chatId}/messages")]
        public async Task<IActionResult> SendMessage(int chatId, [FromBody] MessageDTO dto)
        {
            try
            {
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
