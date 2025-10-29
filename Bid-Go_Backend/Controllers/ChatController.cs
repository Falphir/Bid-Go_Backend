using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.DTOs;
using Bid_Go_Backend.Data.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bid_Go_Backend.Controllers
{
    //[Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly IChatRepository _chatRepository;

        public ChatController(IChatRepository chatRepository)
        {
            _chatRepository = chatRepository;
        }

        //Enviar uma mensagem dentro de um chat específico.
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
                        //DriverId = m.DriverId,
                        //CompanyId = m.CompanyId
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

        //Listar todas as mensagens trocadas num chat específico.
        //[Authorize]
        [HttpPost("{chatId}/messages")]
        public async Task<IActionResult> SendMessage(int chatId, [FromBody] MessageDTO dto)
        {
            // 🔧 Mock do userId (enquanto não tens JWT)
            int userId = 3; // simula o ID do utilizador autenticado

            // Obter o chat e o pedido associado
            var chat = await _chatRepository.GetChatByIdWithRequestAsync(chatId);
            if (chat == null)
                return NotFound(new { message = "Chat não encontrado." });

            // Obter a Bid aceite
            var acceptedBid = await _chatRepository.GetAcceptedBidAsync(chat.TransportRequestId);
            if (acceptedBid == null)
                return BadRequest(new { message = "Não existe bid aceite para este pedido." });

            // Verificar se o user faz parte do chat
            var isCompany = chat.TransportRequest.CompanyId == userId;
            var isDriver = acceptedBid.DriverId == userId;

            if (!isCompany && !isDriver)
                return Forbid("Apenas participantes do pedido podem enviar mensagens.");

            // Obter nome do utilizador consoante o tipo
            string senderName;
            string senderType;

            if (isCompany)
            {
                var company = await _chatRepository.GetCompanyByIdAsync(userId);
                senderName = company?.Name ?? "Empresa";
                senderType = "Company";
            }
            else
            {
                var driver = await _chatRepository.GetDriverByIdAsync(userId);
                senderName = driver?.Name ?? "Motorista";
                senderType = "Driver";
            }

            // Criar e guardar a mensagem
            var message = new Message
            {
                ChatId = chatId,
                Context = dto.Context,
                DriverId = isDriver ? userId : acceptedBid.DriverId,
                CompanyId = isCompany ? userId : chat.TransportRequest.CompanyId
            };

            var result = await _chatRepository.SendMessageAsync(message);

            // Resposta com info enriquecida
            var response = new MessageResponseDTO
            {
                Context = result.Context,
                TimeStamp = result.TimeStamp,
                SenderName = senderName,
                SenderType = senderType
            };

            return Ok(response);
        }



        //Listar todas as mensagens trocadas num chat específico.
        [HttpGet("{chatId}/messages")]
        public async Task<IActionResult> GetMessages(int chatId)
        {
            var messages = await _chatRepository.GetMessagesAsync(chatId);
            return Ok(messages);
        }

        //Criar automaticamente um chat quando uma bid é aceite.
        [HttpPost("create/{transportRequestId}")]
        public async Task<IActionResult> CreateChatFromAcceptedBid(int transportRequestId)
        {
            var chat = await _chatRepository.CreateChatFromAcceptedBidAsync(transportRequestId);
            return Ok(chat);
        }
    }
}
