using Bid_Go_Backend.Data.Models.DTOs;
using Bid_Go_Backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Bid_Go_Backend.Controllers
{
    [ApiController]
    [Route("api/transports")]
    public class TransportUpdateStatusController : ControllerBase
    {
        private readonly ITransportUpdateStatusService _service;
        private readonly IAuthorizationService _authorizationService;

        public TransportUpdateStatusController(ITransportUpdateStatusService service, IAuthorizationService authorizationService)
        {
            _service = service;
            _authorizationService = authorizationService;
        }


        [HttpPut("updateStatus/{requestId}")]
        public async Task<IActionResult> UpdateRequestStatus(int requestId, [FromBody] RequestStatusDTO dto)
        {
            var result = await _service.UpdateRequestStatusAsync(requestId, User, dto.Status);
            return StatusCode(result.StatusCode, result.Body);
        }



        [HttpPut("canceled/{requestId}")]
        public async Task<IActionResult> CancelRequestStatus(int requestId)
        {
            try
            {
                var dto = new RequestStatusDTO { Status = Data.Models.Enums.ERequestStatus.Canceled };
                var result = await _service.UpdateRequestStatusAsync(requestId, User, dto.Status);

                if (result.StatusCode !=200)
                    return StatusCode(result.StatusCode, result.Body);

                return Ok(new
                {
                    message = "Pedido cancelado com sucesso.",
                    data = result.Body
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
