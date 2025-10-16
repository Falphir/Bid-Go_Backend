using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Repositories.BidRepo;
using Bid_Go_Backend.Repositories.Interface;
using Microsoft.AspNetCore.Mvc;
using Bid_Go_Backend.Data.Models.DTOs;

namespace Bid_Go_Backend.Controllers
{
    [ApiController]
    [Route("api/bids")] // Rota raiz
    public class BidsController : ControllerBase
    {

        private readonly IBidCRUD _bidCrud;

        public BidsController(IBidCRUD bidCrud)
        {
            _bidCrud = bidCrud;
        }


        [HttpPost]
        public async Task<IActionResult> AddBid([FromBody] BidDTO bidDto)
        {
            if (bidDto.Value <= 0 || bidDto.DeliveryDeadline <= DateTime.Now)

                return BadRequest("Bid value must be greater than zero.");


            var bid = new Bid
            {
                Value = bidDto.Value,
                DeliveryDeadline = bidDto.DeliveryDeadline,
                Status = bidDto.Status,
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
