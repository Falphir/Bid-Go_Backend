using Bid_Go_Backend.Data;
using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.DTOs;
using Bid_Go_Backend.Data.Models.Enums;
using Bid_Go_Backend.Data.Repositories.Interfaces;
using Bid_Go_Backend.Repositories.Interface;
using Bid_Go_Backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Bid_Go_Backend.Controllers
{
    [ApiController]
    [Route("api/bids/manual")]
    public class AcceptAndRejectBidManualController : ControllerBase
    {
        private readonly IAcceptAndRejectBidManualService _service;

        public AcceptAndRejectBidManualController(IAcceptAndRejectBidManualService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetBidsById([FromQuery] int bidId)
        {
            var bid = await _service.GetBidByIdAsync(bidId);
            if (bid == null)
                return NotFound("No bids found for the given transport request ID.");
            return Ok(bid);
        }

        [HttpGet("by-request/{transportRequestId}")]
        public async Task<IActionResult> GetBidsByTransportRequest(int transportRequestId)
        {
            var bids = await _service.GetBidsByTransportRequestAsync(transportRequestId);
            if (!bids.Any())
                return NotFound("No bids found for the given transport request ID.");
            return Ok(bids);
        }

        [HttpGet("by-request/{transportRequestId}/status/{status}")]
        public async Task<IActionResult> GetBidsByTransportRequestAndStatus(int transportRequestId, EBidStatus status)
        {
            var bids = await _service.GetBidsByTransportRequestAndStatusAsync(transportRequestId, status);
            if (!bids.Any())
                return NotFound("No bids found for the given transport request ID and status.");
            return Ok(bids);
        }

        [HttpPost("{id}/accept")]
        public async Task<IActionResult> AcceptBid(int id)
        {
            try
            {
                await _service.AcceptBidAsync(id);
                return Ok(new { message = "Bid successfully accepted" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("{id}/reject")]
        public async Task<IActionResult> RejectBid(int id)
        {
            try
            {
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