using Bid_Go_Backend.Data.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<HistoryController> _logger;

        public HistoryController(IHistoryRepository repository, ILogger<HistoryController> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        [HttpGet("driver/{driverId}")]
        public async Task<IActionResult> GetDriverHistory(int driverId)
        {
            try
            {
                var history = await _repository.GetDriverHistoryAsync(driverId);

                if (history == null || !history.Any())
                {
                    return NotFound(new { message = "Nenhum histórico encontrado para este motorista." });
                }

                return Ok(history);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ocorreu um erro ao obter o histórico do motorista." });
            }
        }

        [HttpGet("company/{companyId}")]
        public async Task<IActionResult> GetCompanyHistory(int companyId)
        {
            try
            {
                var history = await _repository.GetTransportHistoryAsync(companyId);

                if (history == null || !history.Any())
                {
                    return NotFound(new { message = "Nenhum histórico encontrado para esta empresa." });
                }

                return Ok(history);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ocorreu um erro ao obter o histórico da empresa." });
            }
        }
    }
}
