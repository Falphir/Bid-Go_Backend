using Bid_Go_Backend.Data.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Bid_Go_Backend.Services.Interfaces;

namespace Bid_Go_Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TransportUpdateStatusController : ControllerBase
    {
        private readonly ITransportUpdateStatusService _service;

        public TransportUpdateStatusController(ITransportUpdateStatusService service)
        {
            _service = service;
        }

        [HttpPut("{requestID}/status")]
        public async Task<IActionResult> UpdateRequestStatus(int requestID, int userID, [FromBody] RequestStatusDTO dto)
        {
            try
            {
                var updatedRequest = await _service.UpdateRequestStatusAsync(requestID, userID, dto.Status);

                if (updatedRequest == null)
                    return NotFound(new { message = $"Pedido com ID {requestID} não encontrado." });

                return Ok(new
                {
                    message = $"Estado do pedido atualizado com sucesso para '{dto.Status}'.",
                    data = updatedRequest
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ocorreu um erro ao atualizar o estado do pedido.", error = ex.Message });
            }
        }

        [HttpPut("{requestID}/canceled")]
        public async Task<IActionResult> CancelRequestStatus(int requestID, int userID)
        {
            try
            {
                var dto = new RequestStatusDTO { Status = Data.Models.Enums.ERequestStatus.Canceled };
                var updatedRequest = await _service.UpdateRequestStatusAsync(requestID, userID, dto.Status);

                if (updatedRequest == null)
                    return NotFound(new { message = $"Pedido com ID {requestID} não encontrado." });

                return Ok(new
                {
                    message = "Pedido cancelado com sucesso.",
                    data = updatedRequest
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ocorreu um erro ao cancelar o pedido.", error = ex.Message });
            }
        }
    }
}
