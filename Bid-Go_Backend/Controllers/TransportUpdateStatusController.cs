using Bid_Go_Backend.Data.Models.DTOs;
using Bid_Go_Backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Bid_Go_Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TransportUpdateStatusController : ControllerBase
    {
        private readonly ITransportUpdateStatusService _service;
        private readonly IAuthorizationService _authorizationService;

        public TransportUpdateStatusController(ITransportUpdateStatusService service, IAuthorizationService authorizationService)
        {
            _service = service;
            _authorizationService = authorizationService;
        }


        [HttpPut("{requestId}/status")]
        public async Task<IActionResult> UpdateRequestStatus(int requestId, [FromBody] RequestStatusDTO dto)
        {
            var result = await _service.UpdateRequestStatusAsync(requestId, User, dto.Status);
            return StatusCode(result.StatusCode, result.Body);
        }



        [HttpPut("{requestID}/canceled")]
        public async Task<IActionResult> CancelRequestStatus(int requestID, int userID)
        {

            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "userId")?.Value;
            var roleClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;

            string role = roleClaim;

            try
            {
                var dto = new RequestStatusDTO { Status = Data.Models.Enums.ERequestStatus.Canceled };
                var updatedRequest = await _service.UpdateRequestStatusAsync (requestID, User, dto.Status);

               // if (updatedRequest == null)
                 // return NotFound(new { message = $"Pedido com ID {requestID} não encontrado." });

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
