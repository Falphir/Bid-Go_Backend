using Bid_Go_Backend.Data.Models.DTOs;
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
                var request = await _repository.CreateAsync(dto);
                return CreatedAtAction( nameof(dto.CompanyId),new { id = request.TransportRequestId }, request);
            }
            catch (DbUpdateException dbEx)
            {
                //Erros de BD, como chave estrangeira inválida ou campos nulos
                return BadRequest(new { message = "Erro ao salvar no banco de dados: " + dbEx.InnerException?.Message ?? dbEx.Message });
            }
            catch (ArgumentException argEx)
            {
                // Erros de validação 
                return BadRequest(new { message = argEx.Message });
            }
            catch (Exception ex)
            {
                // Erro inesperado
                return BadRequest(new { message = ex.Message });
            }
        }

       

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateTransportRequestDTO dto)
        {
            try
            {
                var updated = await _repository.UpdateAsync(id, dto);
                if (updated == null)
                    return NotFound(new { message = "Pedido não encontrado." });

                return Ok(updated);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var result = await _repository.DeleteAsync(id);
                if (!result)
                    return NotFound(new { message = "Pedido não encontrado." });

                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // public async Task<IActionResult> GetById(int id)
        // {
        // Este endpoint serve apenas para o CreatedAtAction
        // return Ok(new { message = $"Simulação: pedido {id}" });
        // }
    }
}
