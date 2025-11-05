using Bid_Go_Backend.Data.Models.DTOs;
using Bid_Go_Backend.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Bid_Go_Backend.Controllers
{
    [ApiController]
    [Route("api/transport")]
    public class TransportRequestsController : ControllerBase
    {
        private readonly ITransportRequestService _service;

        public TransportRequestsController(ITransportRequestService service)
        {
            _service = service;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromForm] CreateTransportRequestDTO dto, IFormFile image)
        {
            try
            {
                var created = await _service.CreateAsync(dto, image);
                return CreatedAtAction(nameof(GetById), new { id = created.TransportRequestId }, created);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromForm] UpdateTransportRequestDTO dto, IFormFile? image)
        {
            try
            {
                var updated = await _service.UpdateAsync(id, dto, image);
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
                await _service.DeleteAsync(id);
                return Ok(new { message = "Pedido eliminado com sucesso." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var request = await _service.GetByIdAsync(id);
            if (request == null)
                return NotFound(new { message = "Pedido não encontrado." });

            return Ok(request);
        }

        [HttpGet("company/{companyId}")]
        public async Task<IActionResult> GetByCompany(int companyId)
        {
            var requests = await _service.GetByCompanyAsync(companyId);
            return Ok(requests);
        }
    }
}
