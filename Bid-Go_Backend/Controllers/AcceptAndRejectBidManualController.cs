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


        [Authorize(Policy = "CompanyOnly")]
        [HttpGet("by-request/{transportRequestId}")]
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

        [Authorize(Policy = "CompanyOnly")]
        [HttpGet("by-request/{transportRequestId}/status/{status}")]
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