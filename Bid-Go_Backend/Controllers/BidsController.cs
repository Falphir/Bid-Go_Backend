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
    public class BidsController : ControllerBase
    {

        private readonly IBidCRUD _bidCrud;
        private readonly BidGoDbContext _ctx;
        public BidsController(IBidCRUD bidCrud, BidGoDbContext ctx)
        {
            _bidCrud = bidCrud;
            _ctx = ctx;
        }


        // GET /licitacoes?bid={id}
        [HttpGet]
        public async Task<IActionResult> GetBidsById([FromQuery] int bidId)
        {
            var bids = await _bidCrud.GetBidByIdAsync(bidId);
            if (bids == null)
                return NotFound("No bids found for the given transport request ID.");
            return Ok(bids);
        }


        // GET /api/bids?transportRequestId=X
        [HttpGet("by-request/{transportRequestId}")]
        public async Task<IActionResult> GetBidsByTransportRequest(int transportRequestId)
        {

            var bids = await _bidCrud.GetBidByTransportRequestAsync(transportRequestId);

            if (bids == null || !bids.Any())
                return NotFound("No bids found for the given transport request ID.");
            return Ok(bids);
        }

        // GET /api/bids?transportRequestId=X&status=Y
        [HttpGet("by-request/{transportRequestId}/status/{status}")]
        public async Task<IActionResult> GetBidsByTransportRequestAndStatus(int transportRequestId, EBidStatus status)
        {

            var bids = await _bidCrud.GetBidByTransportRequestAndStatusAsync(transportRequestId, status);

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
                await _bidCrud.AcceptBidAsync(id);
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
                await _bidCrud.RejectBidAsync(id);
                return Ok(new { message = "Bid successfully rejected" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

    }
}
