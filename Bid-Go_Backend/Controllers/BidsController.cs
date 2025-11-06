using Bid_Go_Backend.Data.Models.DTOs;
using Bid_Go_Backend.Data.Models.Enums;
using Bid_Go_Backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using IAuthorizationService = Bid_Go_Backend.Services.Interfaces.IAuthorizationService;

namespace Bid_Go_Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BidsController : ControllerBase
    {
        private readonly IAuthorizationService _authorizationService;
        private readonly IBidsService _service;

        public BidsController(IBidsService service, IAuthorizationService authorizationService)
        {
            _service = service;
            _authorizationService = authorizationService;
        }

        [Authorize(Policy = "DriverOnly")]
        [HttpPost]
        public async Task<IActionResult> AddBid([FromBody] AddBidDTO bidDto)
        {
            var result = await _service.AddBidAsync(bidDto);
            if (!result.Success)
                return BadRequest(result.Message);

            return CreatedAtAction(nameof(AddBid), new { id = result.Bid!.BidId }, result.Bid);
        }

        [Authorize(Policy = "DriverOnly")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateBid(int id, [FromBody] BidUpdateDTO dto)
        {

            var driverId = int.Parse(User.FindFirst("userId")?.Value);

            var ownsBid = await _authorizationService.DriverOwnsBidAsync(driverId, id);
            if (!ownsBid)
                return Forbid();


            var result = await _service.UpdateBidAsync(id, dto);
            if (!result.Success)
                return BadRequest(result.Message);

            return Ok(result.Bid);
        }

        [Authorize(Policy = "DriverOnly")]
        [HttpPatch("{id}/cancel")]
        public async Task<IActionResult> CancelBid(int id)
        {

            var driverId = int.Parse(User.FindFirst("userId")?.Value);

            var ownsBid = await _authorizationService.DriverOwnsBidAsync(driverId, id);
            if (!ownsBid)
                return Forbid();


            var result = await _service.CancelBidAsync(id);
            if (!result.Success)
                return BadRequest(result.Message);

            return Ok(result.Message);
        }

        [Authorize]
        [HttpGet("{bidId}")]
        public async Task<IActionResult> GetBidById(int bidId)
        {
            var bid = await _service.GetBidByIdAsync(bidId);
            if (bid == null)
                return NotFound("Bid not found.");
            return Ok(bid);
        }

        [Authorize]
        [HttpGet("by-request/{transportRequestId}")]
        public async Task<IActionResult> GetBidsByTransportRequest(int transportRequestId)
        {
            var bids = await _service.GetBidsByTransportRequestAsync(transportRequestId);
            if (!bids.Any())
                return NotFound("No bids found.");
            return Ok(bids);
        }

        [Authorize]
        [HttpGet("active")]
        public async Task<IActionResult> GetActiveBids([FromQuery] int transportRequestId, [FromQuery] string orderBy = "value", [FromQuery] bool descending = false)
        {
            var activeBids = await _service.GetActiveBidsAsync(transportRequestId, orderBy, descending);
            var list = activeBids.Select(b => new
            {
                b.BidId,
                b.Value,
                b.DeliveryDeadline,
                Driver = new { b.DriverId, b.Driver.Name, b.Driver.Email }
            }).ToList<object>();
            return Ok(list);
        }
    }
}