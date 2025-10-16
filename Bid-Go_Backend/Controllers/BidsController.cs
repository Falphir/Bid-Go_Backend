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


        [HttpPost]
        public async Task<IActionResult> AddBid([FromBody] BidDTO bidDto)
        {
            var transportRequest = await _ctx.TransportRequests
                .AsNoTracking()
                .FirstOrDefaultAsync(tr => tr.TransportRequestId == bidDto.TransportRequestId);



            if (bidDto.Value <= 0 )

                return BadRequest("Bid value must be greater than zero .");

            if (transportRequest == null)
                return NotFound("Transport request not found.");

            if (bidDto.DeliveryDeadline <= transportRequest.PickupDate)
            {
                return BadRequest("The bid's delivery deadline need to be later than the transport request's pickup Date ");
            }

            if (bidDto.DeliveryDeadline > transportRequest.DeliveryDate)
                return BadRequest("The bid's delivery deadline cannot be later than the transport request's delivery date.");

            var existingBid = await _ctx.Bids
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.DriverId == bidDto.DriverId && b.TransportRequestId == bidDto.TransportRequestId);

            if (existingBid != null && existingBid.Status != EBidStatus.Canceled)
                return Conflict("You have already submitted a bid for this transport request. Please update it instead.");


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


        [HttpPut]
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


        // GET /licitacoes?pedidoId={id}
        [HttpGet]
        public async Task<IActionResult> GetBidsById([FromQuery] int bidId)
        {
            var bids = await _bidCrud.GetBidByIdAsync(bidId);
            if (bids == null)
                return NotFound("No bids found for the given transport request ID.");
            return Ok(bids);
        }

        // DELETE /licitacoes/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> CancelBid(int id)
        {
            var success = await _bidCrud.CancelBidAsync(id);
            if (!success)
                return BadRequest("Licitação não encontrada ou já cancelada.");
            return Ok("Licitação cancelada com sucesso.");
        }
    }
}
