using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.DTOs;
using Bid_Go_Backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using IAuthorizationService = Bid_Go_Backend.Services.Interfaces.IAuthorizationService;

namespace Bid_Go_Backend.Controllers
{
    [ApiController]
    [Route("api/transport")]
    public class TransportRequestsController : ControllerBase
    {
        private readonly ITransportRequestService _service;
        private readonly IAuthorizationService _authorizationService;

        public TransportRequestsController(ITransportRequestService service, IAuthorizationService authorizationService)
        {
            _service = service;
            _authorizationService = authorizationService;
        }

        [Authorize(Policy = "CompanyOnly")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateTransportRequestDTO dto)
        {

            try
            {
                var created = await _service.CreateAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = created.TransportRequestId }, created);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [Authorize(Policy = "CompanyOnly")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateTransportRequestDTO dto)
        {


            var companyId = int.Parse(User.FindFirst("userId")!.Value);

            var hasPermission = await _authorizationService.CompanyOwnsTransportRequestAsync(companyId, id);
            if (!hasPermission)
                return Forbid();

            try
            {
                var updated = await _service.UpdateAsync(id, dto);
                return Ok(updated);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [Authorize(Policy = "CompanyOnly")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {


            var companyId = int.Parse(User.FindFirst("userId")!.Value);

            var hasPermission = await _authorizationService.CompanyOwnsTransportRequestAsync(companyId, id);
            if (!hasPermission)
                return Forbid();

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
