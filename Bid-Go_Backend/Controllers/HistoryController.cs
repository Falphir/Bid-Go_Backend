using Bid_Go_Backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Bid_Go_Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HistoryController : ControllerBase
    {
        private readonly IHistoryService _service;
        private readonly ILogger<HistoryController> _logger;

        public HistoryController(IHistoryService service, ILogger<HistoryController> logger)
        {
            _service = service;
            _logger = logger;
        }

        [HttpGet("driver/{driverId}")]
        public async Task<IActionResult> GetDriverHistory(int driverId)
        {
            try
            {
                var history = await _service.GetDriverHistoryAsync(driverId);

                if (history == null || history.Count == 0)
                {
                    return NotFound(new { message = "Nenhum histórico encontrado para este motorista." });
                }

                return Ok(history);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching driver history for {DriverId}", driverId);
                return StatusCode(500, new { message = "Ocorreu um erro ao obter o histórico do motorista." });
            }
        }

        [HttpGet("company/{companyId}")]
        public async Task<IActionResult> GetCompanyHistory(int companyId)
        {
            try
            {
                var history = await _service.GetTransportHistoryAsync(companyId);

                if (history == null || history.Count == 0)
                {
                    return NotFound(new { message = "Nenhum histórico encontrado para esta empresa." });
                }

                return Ok(history);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching company history for {CompanyId}", companyId);
                return StatusCode(500, new { message = "Ocorreu um erro ao obter o histórico da empresa." });
            }
        }
    }
}
