using Bid_Go_Backend.Data.Models.DTOs;
using Bid_Go_Backend.Data.Models.Enums;
using Bid_Go_Backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using IAuthorizationService = Bid_Go_Backend.Services.Interfaces.IAuthorizationService;

namespace Bid_Go_Backend.Controllers
{
    [ApiController]
    [Route("api/bids/manual")]
    public class AcceptAndRejectBidManualController : ControllerBase
    {
        private readonly IAcceptAndRejectBidManualService _service;
        private readonly IAuthorizationService _authorizationService;

        public AcceptAndRejectBidManualController(IAcceptAndRejectBidManualService service, IAuthorizationService authorizationService)
        {
            _service = service;
            _authorizationService = authorizationService;
        }

        /// <summary>
        /// Get all bids for a transport request. Restricted to the owning company.
        /// </summary>
        /// <param name="transportRequestId">Transport request identifier.</param>
        /// <returns>List of bids or 404 when none found.</returns>
        [Authorize(Policy = "CompanyOnly")]
        [HttpGet("byrequest/{transportRequestId}")]
        public async Task<IActionResult> GetBidsByTransportRequest(int transportRequestId)
        {
            var companyId = int.Parse(User.FindFirst("userId")!.Value);

            var hasPermission = await _authorizationService.CompanyOwnsTransportRequestAsync(companyId, transportRequestId);
            if (!hasPermission)
                return Forbid();

            var bids = await _service.GetBidsByTransportRequestAsync(transportRequestId);
            if (!bids.Any())
                return NotFound("No bids found for the given transport request ID.");

            return Ok(bids);
        }

        /// <summary>
        /// Get bids filtered by transport request and status. Restricted to the owning company.
        /// </summary>
        [Authorize]
        [HttpGet("byrequest/{transportRequestId}/{status}")]
        public async Task<IActionResult> GetBidsByTransportRequestAndStatus(int transportRequestId, EBidStatus status)
        {
            var companyId = int.Parse(User.FindFirst("userId")!.Value);

            var hasPermission = await _authorizationService.CompanyOwnsTransportRequestAsync(companyId, transportRequestId);
            if (!hasPermission)
                return Forbid();


            var bids = await _service.GetBidsByTransportRequestAndStatusAsync(transportRequestId, status);
            if (!bids.Any())
                return NotFound("No bids found for the given transport request ID and status.");
            return Ok(bids);
        }

        /// <summary>
        /// Accept a bid. The controller checks ownership and delegates acceptance logic to the service layer.
        /// </summary>
        [Authorize(Policy = "CompanyOnly")]
        [HttpPost("{id}/accept")]
        public async Task<IActionResult> AcceptBid(int id)
        {
            try
            {
                var companyId = int.Parse(User.FindFirst("userId")!.Value);

                var bid = await _service.GetBidByIdAsync(id);
                if (bid == null)
                    return NotFound("Bid not found.");

                var hasPermission = await _authorizationService.CompanyOwnsTransportRequestAsync(companyId, bid.TransportRequestId);
                if (!hasPermission)
                    return Forbid();

                await _service.AcceptBidAsync(id);
                return Ok(new { message = "Bid successfully accepted" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Reject a bid. The controller checks ownership and delegates rejection logic to the service layer.
        /// </summary>
        [Authorize(Policy = "CompanyOnly")]
        [HttpPost("{id}/reject")]
        public async Task<IActionResult> RejectBid(int id)
        {
            try
            {
                var companyId = int.Parse(User.FindFirst("userId")!.Value);

                var bid = await _service.GetBidByIdAsync(id);
                if (bid == null)
                    return NotFound("Bid not found.");

                var hasPermission = await _authorizationService.CompanyOwnsTransportRequestAsync(companyId, bid.TransportRequestId);
                if (!hasPermission)
                    return Forbid();


                await _service.RejectBidAsync(id);
                return Ok(new { message = "Bid successfully rejected" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }

}