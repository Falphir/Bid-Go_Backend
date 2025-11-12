using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.DTOs;
using Bid_Go_Backend.Data.Models.Enums;
using Bid_Go_Backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using IAuthorizationService = Bid_Go_Backend.Services.Interfaces.IAuthorizationService;

namespace Bid_Go_Backend.Controllers
{
    [ApiController]
    [Route("api/transports")]
    public class TransportRequestsController : ControllerBase
    {
        private readonly ITransportRequestService _service;
        private readonly IAuthorizationService _authorizationService;

        public TransportRequestsController(ITransportRequestService service, IAuthorizationService authorizationService)
        {
            _service = service;
            _authorizationService = authorizationService;
        }

        /// <summary>
        /// Create a new transport request.
        /// </summary>
        /// <remarks>
        /// The endpoint accepts multipart form data to support an optional image upload.
        /// Creation logic and validation are handled by the service layer.
        /// </remarks>
        /// <param name="dto">Transport request creation payload.</param>
        /// <param name="image">Optional image file associated with the request.</param>
        /// <returns>201 Created with the created transport request or 400 on validation error.</returns>
        [Authorize(Policy = "CompanyOnly")]
        [HttpPost("createTransport")]
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



        [Authorize(Policy = "CompanyOnly")]
        [HttpPost("createDRAFTTransport")]
        public async Task<IActionResult> CreateDraft([FromForm] CreateTransportRequestDTO dto, IFormFile image)
        {
            var companyId = int.Parse(User.FindFirst("userId")!.Value);

            try
            {
                var created = await _service.CreateDraftAsync(companyId, dto, image);
                return CreatedAtAction(nameof(GetById), new { id = created.TransportRequestId }, created);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }





        /// <summary>
        /// Update an existing transport request. Caller must be the owning company.
        /// </summary>
        /// <param name="id">Transport request identifier.</param>
        /// <param name="dto">Updated fields for the transport request.</param>
        /// <param name="image">Optional new image file.</param>
        /// <returns>Updated transport request or appropriate error.</returns>
        [Authorize(Policy = "CompanyOnly")]
        [HttpPut("updateTransport/{id}")]
        public async Task<IActionResult> Update(int id, [FromForm] UpdateTransportRequestDTO dto, IFormFile? image)
        {
            var companyId = int.Parse(User.FindFirst("userId")!.Value);

            var hasPermission = await _authorizationService.CompanyOwnsTransportRequestAsync(companyId, id);
            if (!hasPermission)
                return Forbid();

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

        /// <summary>
        /// Delete a transport request. Caller must be the owning company.
        /// </summary>
        /// <param name="id">Transport request identifier.</param>
        /// <returns>Success message or error.</returns>
        [Authorize(Policy = "CompanyOnly")]
        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var companyId = int.Parse(User.FindFirst("userId")!.Value);

            var hasPermission = await _authorizationService.CompanyOwnsTransportRequestAsync(companyId, id);
            if (!hasPermission)
                return Forbid();

            try
            {
                await _service.DeleteAsync(id);
                return Ok(new { message = "Transport request deleted successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Get a transport request by id.
        /// </summary>
        /// <param name="id">Transport request identifier.</param>
        /// <returns>Transport request details or 404 if not found.</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var request = await _service.GetByIdAsync(id);
            if (request == null)
                return NotFound(new { message = "Transport request not found." });

            return Ok(request);
        }

        /// <summary>
        /// Get all transport requests for a specific company.
        /// </summary>
        /// <param name="companyId">Company identifier.</param>
        /// <returns>List of transport requests for the company.</returns>
        [HttpGet("company/{companyId}")]
        public async Task<IActionResult> GetByCompany(int companyId)
        {
            var requests = await _service.GetByCompanyAsync(companyId);
            return Ok(requests);
        }

        [Authorize(Policy = "CompanyOnly")]
        [HttpPut("company/publish/{id}")]
        public async Task<IActionResult> PublishAsync(int id)
        {
            var companyId = int.Parse(User.FindFirst("userId")!.Value);

            if (!await _authorizationService.CompanyOwnsTransportRequestAsync(companyId, id))
                return Forbid();

            var request = await _service.GetByIdAsync(id);
            if (request == null)
                return NotFound(new { message = "Pedido não encontrado." });

            if (request.Status != ERequestStatus.Draft)
                return BadRequest(new { message = "Apenas pedidos em DRAFT podem ser publicados." });

            var updated = await _service.PublishAsync(id); 
            return Ok(updated);
        }


    }
}
