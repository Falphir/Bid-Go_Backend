using Bid_Go_Backend.Data.Models.DTOs;
using Bid_Go_Backend.Data.Models.Enums;
using Bid_Go_Backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using IAuthorizationService = Bid_Go_Backend.Services.Interfaces.IAuthorizationService;

namespace Bid_Go_Backend.Controllers
{
    [ApiController]
    [Route("api/bids")]
    public class BidsController : ControllerBase
    {
        private readonly IAuthorizationService _authorizationService;
        private readonly IBidsService _service;

        public BidsController(IBidsService service, IAuthorizationService authorizationService)
        {
            _service = service;
            _authorizationService = authorizationService;
        }

        /// <summary>
        /// Create a new Bid for a transport request.
        /// </summary>
        /// <remarks>
        /// The driverId is taken from the JWT token. Controllers should remain thin: validation and business rules are implemented in the service layer.
        /// </remarks>
        /// <param name="dto">Bid creation DTO</param>
        /// <returns>Created bid object or error message</returns>
        [Authorize(Policy = "DriverOnly")]
        [HttpPost("createbid")]
        public async Task<IActionResult> AddBid([FromBody] AddBidDTO dto)
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "userId")?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized(new { message = "Invalid token: missing userId." });

            int driverId = int.Parse(userIdClaim);

            var result = await _service.AddBidAsync(driverId, dto);

            if (!result.Success)
                return BadRequest(result.Message);

            return Ok(result.Bid);
        }

        /// <summary>
        /// Update an existing bid owned by the authenticated driver.
        /// </summary>
        /// <param name="id">Bid identifier</param>
        /// <param name="dto">Fields to update</param>
        /// <returns>Updated bid or error</returns>
        [Authorize(Policy = "DriverOnly")]
        [HttpPut("updatebid/{id}")]
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

        /// <summary>
        /// Cancel a bid owned by the authenticated driver.
        /// </summary>
        /// <param name="id">Bid identifier</param>
        /// <returns>Success message or error</returns>
        [Authorize(Policy = "DriverOnly")]
        [HttpPatch("cancel/{id}")]
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

        /// <summary>
        /// Get a bid by its identifier.
        /// </summary>
        /// <param name="bidId">Bid identifier</param>
        /// <returns>Bid details or 404</returns>
        [Authorize]
        [HttpGet("{bidId}")]
        public async Task<IActionResult> GetBidById(int bidId)
        {
            var bid = await _service.GetBidByIdAsync(bidId);
            if (bid == null)
                return NotFound("Bid not found.");
            return Ok(bid);
        }

        /// <summary>
        /// Get all bids for a transport request.
        /// </summary>
        /// <param name="transportRequestId">Transport request identifier</param>
        /// <returns>List of bids or 404 when none found</returns>
        [Authorize]
        [HttpGet("byrequest/{transportRequestId}")]
        public async Task<IActionResult> GetBidsByTransportRequest(int transportRequestId)
        {
            var bids = await _service.GetBidsByTransportRequestAsync(transportRequestId);
            if (!bids.Any())
                return NotFound("No bids found.");
            return Ok(bids);
        }

        /// <summary>
        /// Get active bids for a transport request with optional ordering.
        /// </summary>
        /// <param name="transportRequestId">Transport request identifier</param>
        /// <param name="orderBy">Field to order by (default: value)</param>
        /// <param name="descending">Whether to sort descending</param>
        /// <returns>List of active bids with driver summary</returns>
        [Authorize]
        [HttpGet("bidsActive")]
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

        /// <returns>List of bids from one driver summary</returns>
        [Authorize]
        [HttpGet("bidsByDriver/{driverId}")]
        public async Task<IActionResult> GetBidsByDriverId(int driverId){
            var bidsByDriver = await _service.GetBidsByDriverId(driverId);
            var list = bidsByDriver.Select(b => new
            {
                b.BidId,
                b.Value,
                b.DeliveryDeadline,
                b.TransportRequestId,
                b.Status,
                TransportRequest = new { b.TransportRequest.Status}
            }).ToList<object>();
            return Ok(list);
        }
    }
}