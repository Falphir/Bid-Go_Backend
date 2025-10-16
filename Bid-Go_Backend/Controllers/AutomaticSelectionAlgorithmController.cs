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

            if (result == null)
                return NotFound("Nenhuma proposta elegível encontrada para este pedido.");

            return Ok(new
            {
                message = "Seleção automática executada com sucesso.",
                selectedBid = result
            });
        }
    }
}
