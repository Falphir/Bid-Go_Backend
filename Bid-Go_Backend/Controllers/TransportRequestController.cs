using Bid_Go_Backend.Data.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Bid_Go_Backend.Data.Repositories.Interfaces;

namespace Bid_Go_Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TransportRequestController : ControllerBase
    {
        private readonly ITransportRequestRepository _repository;

        public TransportRequestController(ITransportRequestRepository repository)
        {
            _repository = repository;
        }

        [HttpPut("{requestID}/status")]
        public async Task<IActionResult> UpdateRequestStatus(int requestID, int companyID, [FromBody] RequestStatusDTO dto)
        {
            try
            {
                var updatedRequest = await _repository.UpdateRequestStatusAsync(requestID, companyID, dto.Status);

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
        public async Task<IActionResult> CancelRequestStatus(int requestID)
        {
            try
            {
                var dto = new RequestStatusDTO { Status = Data.Models.Enums.ERequestStatus.Canceled };
                var updatedRequest = await _repository.UpdateRequestStatusAsync(requestID, dto.Status);

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
