using Bid_Go_Backend.Data.Models.DTOs;
using Bid_Go_Backend.Data.Repositories.Interfaces;
using Bid_Go_Backend.Data.Repositories.Transport_Request;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bid_Go_Backend.Controllers
{

    [ApiController]
    [Route("api/PageRequests")]
    public class TransportRequestsPageController : ControllerBase
    {
        private readonly ITransportRequestsPageRepository _repository;

        public TransportRequestsPageController(ITransportRequestsPageRepository repository)
        {
            _repository = repository;
        }

        // GET: api/PageRequests
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TransportRequestsPageDTO>>> GetActive(
            [FromQuery] string? origin,
            [FromQuery] string? destination,
            [FromQuery] DateTime? deliveryDate
        )
        {
            var requests = await _repository.GetActiveAsync(origin, destination, deliveryDate);

            if (!requests.Any())
                return Ok(new { message = "Não existem pedidos ativos no momento." });

            var dtoList = requests.Select(tr => new TransportRequestsPageDTO
            {
              
                Origin = tr.Origin,
                Destination = tr.Destination,
                Package = tr.Package,
                PickupDate = tr.PickupDate,
                DeliveryDate = tr.DeliveryDate,
                Image = tr.Image
            });

            return Ok(dtoList);
        }

        // GET: api/PageRequests/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<TransportRequestResponseDTO>> GetById(int id)
        {
            try
            {
                var alvo = await _repository.GetByIdAsync(id);

                if (alvo == null)
                    return NotFound("Pedido de transporte não existe");

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
                    Image = alvo.Image
                };

                return Ok(responseDto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erro inesperado: " + ex.Message });
            }

        }
    }
}