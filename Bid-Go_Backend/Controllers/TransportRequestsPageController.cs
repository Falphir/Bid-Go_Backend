using Bid_Go_Backend.Data.Models.DTOs;
using Bid_Go_Backend.Services.Transport_Request;
using Microsoft.AspNetCore.Mvc;

namespace Bid_Go_Backend.Controllers
{
    [ApiController]
    [Route("api/PageRequests")]
    public class TransportRequestsPageController : ControllerBase
    {
        private readonly ITransportRequestsPageService _service;

        public TransportRequestsPageController(ITransportRequestsPageService service)
        {
            _service = service;
        }

        // GET: api/PageRequests
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TransportRequestsPageDTO>>> GetActive(
            [FromQuery] string? origin,
            [FromQuery] string? destination,
            [FromQuery] DateTime? deliveryDate,
            [FromQuery] string? priceOrder)
        {
            try
            {
                var requests = await _service.GetActiveAsync(origin, destination, deliveryDate, priceOrder);

                if (!requests.Any())
                    return Ok(new { message = "Não existem pedidos ativos no momento." });

                var dtoList = requests.Select(tr => new TransportRequestsPageDTO
                {
                    Origin = tr.Origin,
                    Destination = tr.Destination,
                    Package = tr.Package,
                    PickupDate = tr.PickupDate,
                    DeliveryDate = tr.DeliveryDate,
                    Image = tr.Image,
                    MaxPrice = tr.MaxPrice
                });

                return Ok(dtoList);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erro inesperado: " + ex.Message });
            }
        }

        // GET: api/PageRequests/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<TransportRequestResponseDTO>> GetById(int id)
        {
            try
            {
                var alvo = await _service.GetByIdAsync(id);

                if (alvo == null)
                    return NotFound(new { message = "Pedido de transporte não existe." });

                var responseDto = new TransportRequestResponseDTO
                {
                    Origin = alvo.Origin,
                    Destination = alvo.Destination,
                    Package = alvo.Package,
                    PickupDate = alvo.PickupDate,
                    DeliveryDate = alvo.DeliveryDate,
                    Weight = alvo.Weight,
                    Volume = alvo.Volume,
                    Length = alvo.Length,
                    Width = alvo.Width,
                    Height = alvo.Height,
                    MaxPrice = alvo.MaxPrice,
                    Image = alvo.Image
                };

                return Ok(responseDto);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erro inesperado: " + ex.Message });
            }
        }
    }
}
