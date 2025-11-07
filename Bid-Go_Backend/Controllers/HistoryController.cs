using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Bid_Go_Backend.Controllers
{
    [ApiController]
    [Route("api/history")]
    public class HistoryController : ControllerBase
    {
        private readonly IHistoryService _service;
        private readonly ILogger<HistoryController> _logger;

        public HistoryController(IHistoryService service, ILogger<HistoryController> logger)
        {
            _service = service;
            _logger = logger;
        }

        /// <summary>
        /// Get the driving history for a driver.
        /// </summary>
        /// <param name="driverId">Driver identifier. Caller must be the same driver.</param>
        /// <returns>Driver history entries or 404 when none found.</returns>
        [Authorize(Policy = "DriverOnly")]
        [HttpGet("driver/{driverId}")]
        public async Task<IActionResult> GetDriverHistory(int driverId)
        {

            var userId = int.Parse(User.FindFirst("userId")!.Value);

            if(userId!= driverId)
            {
                return Forbid();

            }

            try
            {
                var history = await _service.GetDriverHistoryAsync(driverId);

                if (history == null || history.Count == 0)
                {
                    return NotFound(new { message = "No history found for this driver." });
                }

                return Ok(history);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching driver history for {DriverId}", driverId);
                return StatusCode(500, new { message = "An error occurred while fetching driver history." });
            }
        }

        /// <summary>
        /// Get the transport history for a company.
        /// </summary>
        /// <param name="companyId">Company identifier. Caller must be the same company.</param>
        /// <returns>Transport history entries or 404 when none found.</returns>
        [Authorize(Policy = "CompanyOnly")]
        [HttpGet("company/{companyId}")]
        public async Task<IActionResult> GetCompanyHistory(int companyId)
        {

            var userId = int.Parse(User.FindFirst("userId")!.Value);

            if (userId != companyId)
            {
                return Forbid();

            }


            try
            {
                var history = await _service.GetTransportHistoryAsync(companyId);

                if (history == null || history.Count == 0)
                {
                    return NotFound(new { message = "No history found for this company." });
                }

                return Ok(history);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching company history for {CompanyId}", companyId);
                return StatusCode(500, new { message = "An error occurred while fetching company history." });
            }
        }
    }
}
