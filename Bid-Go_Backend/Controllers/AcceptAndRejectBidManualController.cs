using Bid_Go_Backend.Data;
using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.DTOs;
using Bid_Go_Backend.Data.Models.Enums;
using Bid_Go_Backend.Repositories.BidRepo;
using Bid_Go_Backend.Repositories.Interface;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Bid_Go_Backend.Controllers
{
    [ApiController]
    [Route("api/bids")] // Rota raiz
    public class AcceptAndRejectBidManualController: ControllerBase
    {

        private readonly IAcceptAndRejectBidManual _AcceptOrReject;
        private readonly BidGoDbContext _ctx;
        public AcceptAndRejectBidManualController(IAcceptAndRejectBidManual AcceptOrReject, BidGoDbContext ctx)
        {
            _AcceptOrReject = AcceptOrReject;
            _ctx = ctx;
        }


        // GET /licitacoes?bid={id}
        [HttpGet]
        public async Task<IActionResult> GetBidsById([FromQuery] int bidId)
        {
            var bids = await _AcceptOrReject.GetBidByIdAsync(bidId);
            if (bids == null)
                return NotFound("No bids found for the given transport request ID.");
            return Ok(bids);
        }


        // GET /api/bids?transportRequestId=X
        [HttpGet("by-request/{transportRequestId}")]
        public async Task<IActionResult> GetBidsByTransportRequest(int transportRequestId)
        {

            var bids = await _AcceptOrReject.GetBidByTransportRequestAsync(transportRequestId);

            if (bids == null || !bids.Any())
                return NotFound("No bids found for the given transport request ID.");
            return Ok(bids);
        }

        // GET /api/bids?transportRequestId=X&status=Y
        [HttpGet("by-request/{transportRequestId}/status/{status}")]
        public async Task<IActionResult> GetBidsByTransportRequestAndStatus(int transportRequestId, EBidStatus status)
        {

            var bids = await _AcceptOrReject.GetBidByTransportRequestAndStatusAsync(transportRequestId, status);

            if (bids == null || !bids.Any())
                return NotFound("No bids found for the given transport request ID and status.");
            return Ok(bids);
        }


        //Post /licitacoes/{id}
        [HttpPost("{id}/accept")]
        public async Task<IActionResult> AcceptBid(int id)
        {
            try
            {
                await _AcceptOrReject.AcceptBidAsync(id);
                return Ok(new { message = "Bid successfully accepted" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        //Post /bid/{id}
        [HttpPost("{id}/reject")]
        public async Task<IActionResult> RejectedBid(int id)
        {
            try
            {
                await _AcceptOrReject.RejectBidAsync(id);
                return Ok(new { message = "Bid successfully rejected" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

    }
}
