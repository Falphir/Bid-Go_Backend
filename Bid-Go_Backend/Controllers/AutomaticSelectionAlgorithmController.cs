using Bid_Go_Backend.Data.Models.DTOs;
using Bid_Go_Backend.Data.Repositories.Interfaces;
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

        [HttpPost("execute/{transportRequestId:int}")]
        public async Task<IActionResult> ExecuteAlgorithm(int transportRequestId)
        {
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
