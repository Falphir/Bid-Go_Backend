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

       

        // GET /api/bids/active?transportRequestId=1&orderBy=value
        [HttpGet("active")]
        public async Task<IActionResult> GetActiveBids(
    [FromQuery] int transportRequestId,
    [FromQuery] string orderBy = "value",
    [FromQuery] bool descending = false)
        {
            var activeBids = await _bidCrud.GetActiveBidsByTransportRequestAsync(transportRequestId, orderBy, descending);

            if (activeBids == null || !activeBids.Any())
                return Ok(new { message = "No active bids found for this request.", bids = new List<object>() });

            var result = activeBids.Select(b => new
            {
                b.BidId,
                b.Value,
                b.DeliveryDeadline,
                Driver = new { b.Driver.Name, b.Driver.Email }
            });

            return Ok(result);
        }
    }
}
    
