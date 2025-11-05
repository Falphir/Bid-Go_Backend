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

        [Authorize(Policy = "CompanyOnly")]
        [HttpPost("execute/{transportRequestId:int}")]
        public async Task<IActionResult> ExecuteAlgorithm(int transportRequestId)
        {


            var companyIdClaim = User.FindFirst("userId")?.Value;
            if (companyIdClaim == null)
                return Unauthorized(new { message = "Token inválido ou utilizador não autenticado." });



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
