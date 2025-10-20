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
            bool result = await repository.SubmitReviewAsync(reviewDTO);
            if (result)
            {
                return Ok(new { message = "Review submitted successfully." });
            }
            else
            {
                return BadRequest(new { message = "Failed to submit review." });
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
