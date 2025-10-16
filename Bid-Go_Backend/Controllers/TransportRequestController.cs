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
        public async Task<IActionResult> UpdateRequestStatus(int requestID, [FromBody] RequestStatusDTO dto)
        {
            try
            {
                var updatedRequest = await _repository.UpdateRequestStatusAsync(requestID, dto);
                return Ok(updatedRequest);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{requestID}/canceled")]
        public async Task<IActionResult> CancelRequestStatus(int requestID)
        {
            try
            {
                var dto = new RequestStatusDTO { Status = Data.Models.Enums.ERequestStatus.Canceled };
                var updatedRequest = await _repository.UpdateRequestStatusAsync(requestID, dto);
                return Ok(updatedRequest);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
