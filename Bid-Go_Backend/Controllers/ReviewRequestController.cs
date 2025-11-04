using Bid_Go_Backend.Data.Models.DTOs;
using Bid_Go_Backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace Bid_Go_Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReviewRequestController : ControllerBase
    {
        private readonly IReviewRequestService _service;

        public ReviewRequestController(IReviewRequestService service)
        {
            _service = service;
        }

        [HttpPost("submit-review")]
        public async Task<IActionResult> SubmitReview([FromBody] ReviewRequestServiceDTO reviewDTO)
        {
            try
            {
                bool result = await _service.SubmitReviewAsync(reviewDTO);
                if (result)
                {
                    return Ok(new { message = "Review submetida com Sucesso." });
                }
                else
                {
                    return BadRequest(new { message = "Erro ao submeter a Review." });
                }
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (ArgumentOutOfRangeException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ocorreu um erro inesperado.", detail = ex.Message });
            }
        }

        [HttpGet("avaliacoes/{request_id}")]
        public async Task<IActionResult> GetReviewsByService(int request_id)
        {
            try
            {
                var reviews = await _service.GetReviewByServiceIdAsync(request_id);
                return Ok(reviews);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ocorreu um erro inesperado.", detail = ex.Message });
            }
        }
    }
}
