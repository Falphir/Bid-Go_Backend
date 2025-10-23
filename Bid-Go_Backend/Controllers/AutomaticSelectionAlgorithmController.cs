using Bid_Go_Backend.Data.Models.DTOs;
using Bid_Go_Backend.Data.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Bid_Go_Backend.Controllers
{
    [ApiController]
    [Route("api/bids")]
    public class AutomaticSelectionAlgorithmController : ControllerBase
    {
        private readonly IAutomaticSelectionAlgorithmRepository _selectionRepo;

        public AutomaticSelectionAlgorithmController(IAutomaticSelectionAlgorithmRepository selectionRepo)
        {
            _selectionRepo = selectionRepo;
        }

        [HttpPost("execute/{transportRequestId}")]
        public async Task<IActionResult> ExecuteAlgorithm(int transportRequestId)
        {
            var result = await _selectionRepo.ExecuteAutomaticSelectionAsync(transportRequestId);

            if (!result.Success)
                return BadRequest(result.Message);

            var DriverDto = new DriverDTO
            {
                Id = result.SelectedBid.DriverId,
                Name = result.SelectedBid.Driver.Name,
                Email = result.SelectedBid.Driver.Email,
                PhoneNumber = result.SelectedBid.Driver.PhoneNumber
            };

            var bidDto = new BidDTO
            {
                BidId = result.SelectedBid.BidId,
                Value = result.SelectedBid.Value,
                Driver = DriverDto
            };

            return Ok(new
            {
                message = "Automatic selection executed successfully.",
                selectedBid = bidDto
            });
        }
    }
}
