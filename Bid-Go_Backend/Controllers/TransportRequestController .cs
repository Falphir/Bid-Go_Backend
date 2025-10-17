using Bid_Go_Backend.Data.Models.DTOs;
using Bid_Go_Backend.Data.Models.Enums;
using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Bid_Go_Backend.Controllers
{
    [ApiController]
    [Route("api/transport")]
    public class TransportRequestsController : ControllerBase
    {
        private readonly ITransportRequestRepository _repository;

        public TransportRequestsController(ITransportRequestRepository repository)
        {
            _repository = repository;
        }

 
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateTransportRequestDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                
                if (dto.PickupDate >= dto.DeliveryDate)
                    return BadRequest(new { message = "A data de recolha deve ser anterior à data de entrega." });

                if (string.IsNullOrWhiteSpace(dto.Image))
                    return BadRequest(new { message = "A imagem é obrigatória para publicar o pedido." });

                if (dto.Weight <= 0 || dto.Volume <= 0)
                    return BadRequest(new { message = "O peso e o volume devem ser superiores a zero." });

                var request = new TransportRequest
                {
                    Origin = dto.Origin,
                    Destination = dto.Destination,
                    Package = dto.Package,
                    Weight = dto.Weight,
                    Volume = dto.Volume,
                    Length = dto.Length,
                    Width = dto.Width,
                    Height = dto.Height,
                    PickupDate = dto.PickupDate,
                    DeliveryDate = dto.DeliveryDate,
                    Image = dto.Image,
                    CompanyId = dto.CompanyId,
                    Status = ERequestStatus.Draft
                };

                var created = await _repository.CreateAsync(request);
                return CreatedAtAction(nameof(Create), new { id = created.TransportRequestId }, created);
            }
            catch (DbUpdateException ex)
            {
                return BadRequest(new { message = "Erro ao gravar no banco de dados: " + (ex.InnerException?.Message ?? ex.Message) });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erro inesperado: " + ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateTransportRequestDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                // Validações 
                if (dto.PickupDate.HasValue && dto.DeliveryDate.HasValue && dto.PickupDate >= dto.DeliveryDate)
                    return BadRequest(new { message = "A data de recolha deve ser anterior à data de entrega." });

                if (dto.Image != null && string.IsNullOrWhiteSpace(dto.Image))
                    return BadRequest(new { message = "A imagem é obrigatória para publicar o pedido." });

                if (dto.Weight.HasValue && dto.Weight <= 0)
                    return BadRequest(new { message = "O peso deve ser superior a zero." });

                if (dto.Volume.HasValue && dto.Volume <= 0)
                    return BadRequest(new { message = "O volume deve ser superior a zero." });

                // Procura o request 
                var existingRequest = await _repository.GetByIdAsync(id);
                if (existingRequest == null)
                    return NotFound(new { message = "Pedido de transporte não existe." });

                if (existingRequest.Status != ERequestStatus.Draft)
                    return BadRequest(new { message = "Só é possível atualizar pedidos com estado DRAFT." });

                // Assim vai atualizar os campos que queremos meter no body
                if (!string.IsNullOrWhiteSpace(dto.Origin))
                    existingRequest.Origin = dto.Origin;

                if (!string.IsNullOrWhiteSpace(dto.Destination))
                    existingRequest.Destination = dto.Destination;

                if (!string.IsNullOrWhiteSpace(dto.Package))
                    existingRequest.Package = dto.Package;

                if (dto.PickupDate.HasValue)
                    existingRequest.PickupDate = dto.PickupDate.Value;

                if (dto.DeliveryDate.HasValue)
                    existingRequest.DeliveryDate = dto.DeliveryDate.Value;

                if (dto.Weight.HasValue)
                    existingRequest.Weight = dto.Weight.Value;

                if (dto.Volume.HasValue)
                    existingRequest.Volume = dto.Volume.Value;

                if (dto.Length.HasValue)
                    existingRequest.Length = dto.Length.Value;

                if (dto.Width.HasValue)
                    existingRequest.Width = dto.Width.Value;

                if (dto.Height.HasValue)
                    existingRequest.Height = dto.Height.Value;

                if (!string.IsNullOrWhiteSpace(dto.Image))
                    existingRequest.Image = dto.Image;

                var updatedRequest = await _repository.UpdateAsync(id, existingRequest);
                return Ok(updatedRequest);
            }
            catch (DbUpdateException ex)
            {
                return BadRequest(new { message = "Erro ao gravar na base de dados: " + (ex.InnerException?.Message ?? ex.Message) });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erro inesperado: " + ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var existing = await _repository.GetByIdAsync(id);
                if (existing == null)
                    return NotFound(new { message = "Pedido não encontrado." });

                if (existing.Status != ERequestStatus.Active)
                    return Conflict(new { message = "Apenas pedidos ativos podem ser eliminados." });

                await _repository.DeleteAsync(id);
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erro ao eliminar o pedido: " + ex.Message });
            }
        }


        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
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
            }catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erro inesperado: " + ex.Message });
            }
           

        }


    }
}
