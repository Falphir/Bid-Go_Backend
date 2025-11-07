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

        /// <summary>
        /// Update the status of a transport request.
        /// </summary>
        /// <remarks>
        /// This endpoint delegates status validation and authorization to the service layer.
        /// The controller focuses on HTTP concerns (status codes and request/response shapes).
        /// </remarks>
        /// <param name="requestId">Identifier of the transport request to update.</param>
        /// <param name="dto">DTO containing the new status.</param>
        /// <returns>Standardized response returned by the service with appropriate HTTP status code.</returns>
        [HttpPut("updateStatus/{requestId}")]
        public async Task<IActionResult> UpdateRequestStatus(int requestId, [FromBody] RequestStatusDTO dto)
        {
            var result = await _service.UpdateRequestStatusAsync(requestId, User, dto.Status);
            return StatusCode(result.StatusCode, result.Body);
        }

        /// <summary>
        /// Cancel a transport request.
        /// </summary>
        /// <remarks>
        /// Uses the same service method as status updates but enforces a cancel status.
        /// Return messages are human-readable for API consumers; prefer machine-readable error codes in the future.
        /// </remarks>
        /// <param name="requestId">Identifier of the transport request to cancel.</param>
        /// <returns>200 OK when canceled or an appropriate error status.</returns>
        [HttpPut("canceled/{requestId}")]
        public async Task<IActionResult> CancelRequestStatus(int requestId)
        {
            try
            {
                var dto = new RequestStatusDTO { Status = Data.Models.Enums.ERequestStatus.Canceled };
                var result = await _service.UpdateRequestStatusAsync(requestId, User, dto.Status);

                if (result.StatusCode != 200)
                    return StatusCode(result.StatusCode, result.Body);

                return Ok(new
                {
                    message = "Transport request successfully canceled.",
                    data = result.Body
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while canceling the transport request.", error = ex.Message });
            }
        }
    }
}
