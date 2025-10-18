using Bid_Go_Backend.Data.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bid_Go_Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HistoryController : ControllerBase
    {
        private readonly IHistoryRepository _repository;

        public HistoryController(IHistoryRepository repository)
        {
            _repository = repository;
        }

        [HttpGet("driver/{driverId}")]
        public async Task<IActionResult> GetDriverHistory(int driverId)
        {
            var history = await _repository.GetDriverHistoryAsync(driverId);
            return Ok(history);
        }
    }
}
