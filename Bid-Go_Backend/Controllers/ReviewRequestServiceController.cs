using Bid_Go_Backend.Data.Models.DTOs;
using Bid_Go_Backend.Data.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bid_Go_Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReviewRequestServiceController : ControllerBase
    {
        private readonly IReviewRequestServiceRepository repository;

        public ReviewRequestServiceController(IReviewRequestServiceRepository repository)
        {
            this.repository = repository;
        }

        [HttpPost("submit-review")]
        public async Task<IActionResult> SubmitReview([FromBody] ReviewRequestServiceDTO reviewDTO)
        {
            try
            {
                bool result = await repository.SubmitReviewAsync(reviewDTO);
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

        //[HttpGet("get-review/{transportRequestId}")]
        //public async Task<IActionResult> GetReviewByServiceId(int transportRequestId)
        //{
        //    var review = await repository.GetReviewByServiceIdAsync(transportRequestId);
        //    if (review != null)
        //    {
        //        return Ok(review);
        //    }
        //    else
        //    {
        //        return NotFound(new { message = "Review not found." });
        //    }
        //}
    }
}
