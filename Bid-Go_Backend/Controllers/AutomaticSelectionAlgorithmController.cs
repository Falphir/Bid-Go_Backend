using Bid_Go_Backend.Data.Models.DTOs;
using Bid_Go_Backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe;

namespace Bid_Go_Backend.Controllers
{
    [ApiController]
    [Route("api/bids")]
    public class AutomaticSelectionAlgorithmController : ControllerBase
    {
        private readonly IAutomaticSelectionAlgorithmService _service;

        public AutomaticSelectionAlgorithmController(IAutomaticSelectionAlgorithmService service)
        {
            _service = service;
        }

        /// <summary>
        /// Execute the automatic selection algorithm for a transport request.
        /// </summary>
        /// <remarks>
        /// The endpoint is restricted to company users. Token validation is handled by the authentication middleware; controller performs basic presence checks.
        /// </remarks>
        /// <param name="transportRequestId">Transport request identifier.</param>
        /// <returns>Selected bid details or an error message.</returns>
        [Authorize(Policy = "CompanyOnly")]
        [HttpPost("execute/{transportRequestId:int}")]
        public async Task<IActionResult> ExecuteAlgorithm(int transportRequestId)
        {
            var companyIdClaim = User.FindFirst("userId")?.Value;
            if (companyIdClaim == null)
                return Unauthorized(new { message = "Invalid token or unauthenticated user." });

            var (success, message, selectedBid) = await _service.ExecuteAsync(transportRequestId);

            if (!success)
                return BadRequest(message ?? "Automatic selection could not be executed.");

            var driverDto = new DriverDTO
            {
                Id = selectedBid!.DriverId,
                Name = selectedBid.Driver!.Name,
                Email = selectedBid.Driver.Email,
                PhoneNumber = selectedBid.Driver.PhoneNumber
            };

            var bidDto = new BidDTO
            {
                BidId = selectedBid.BidId,
                Value = selectedBid.Value,
                Driver = driverDto
            };

            return Ok(new
            {
                message = "Automatic selection executed successfully.",
                selectedBid = bidDto
            });
        }
    }
}
