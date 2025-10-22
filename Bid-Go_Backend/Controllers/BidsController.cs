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

        // POST /api/bids
        [HttpPost]
        public async Task<IActionResult> AddBid([FromBody] BidDTO bidDto)
        {
            var transportRequest = await _ctx.TransportRequests
                .AsNoTracking()
                .FirstOrDefaultAsync(tr => tr.TransportRequestId == bidDto.TransportRequestId);


            if (transportRequest == null)
                return NotFound("Transport request not found.");

            if(transportRequest.Status != ERequestStatus.Active)
                return BadRequest("Cannot place a bid on a transport request that is not open.");

            if (bidDto.Value <= 0 )

                return BadRequest("Bid value must be greater than zero .");

            if (bidDto.DeliveryDeadline <= transportRequest.PickupDate)
            {
                return BadRequest("The bid's delivery deadline need to be later than the transport request's pickup Date ");
            }

            if (bidDto.DeliveryDeadline > transportRequest.DeliveryDate)
                return BadRequest("The bid's delivery deadline cannot be later than the transport request's delivery date.");

            var existingBid = await _ctx.Bids
                .AsNoTracking()
                .Where(b => b.DriverId == bidDto.DriverId && b.TransportRequestId == bidDto.TransportRequestId)
                .ToListAsync();
                    
            bool hasActiveBid = existingBid.Any(b => b.Status != EBidStatus.Canceled);


            if(hasActiveBid)
                return BadRequest("Driver already has an active bid for this transport request.");


            var bid = new Bid
            {
                Value = bidDto.Value,
                DeliveryDeadline = bidDto.DeliveryDeadline,
                DriverId = bidDto.DriverId,
                TransportRequestId = bidDto.TransportRequestId

            };


            var createdBid = await _bidCrud.CreateBidAsync(bid);
            return CreatedAtAction(nameof(AddBid), new { id = createdBid.BidId }, createdBid);

        }


        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateBid(int id, [FromBody] BidUpdateDTO updateDTO)
        {
            if(!ModelState.IsValid)
        return BadRequest(ModelState);

            var bidToUpdate = new Bid
            {

                Value = updateDTO.Value,
                DeliveryDeadline = updateDTO.DeliveryDeadline
            };

            var updatedBid = await _bidCrud.UpdateBidAsync(id, bidToUpdate);

            if (updatedBid == null)
                return NotFound("Bid not found or cannot be updated.");

            return Ok(updatedBid);
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

            if ( bids == null || !bids.Any())
                return NotFound("No bids found for the given transport request ID and status.");
            return Ok(bids);
        }


        // DELETE /licitacoes/{id}
        [HttpPatch("{id}/cancel")]
        public async Task<IActionResult> CancelBid(int id)
        {
            var success = await _bidCrud.CancelBidAsync(id);
            if (!success)
                return NotFound("Only pending bids can be canceled");
            return Ok("Bid canceled successfully.");
        }
    }
}
