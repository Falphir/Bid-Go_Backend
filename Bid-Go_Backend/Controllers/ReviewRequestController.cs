using Bid_Go_Backend.Data.Models.DTOs;
using Bid_Go_Backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using System;
using System.Threading.Tasks;

namespace Bid_Go_Backend.Controllers
{
    [ApiController]
    [Route("api/reviewRequest")]
    [Authorize]
    public class ReviewRequestController : ControllerBase
    {
        private readonly IReviewRequestService _service;

        public ReviewRequestController(IReviewRequestService service)
        {
            _service = service;
        }

        /// <summary>
        /// Submit a review for a transport request or driver/company.
        /// </summary>
        /// <remarks>
        /// Validation and business rules are handled in the service layer; controller translates exceptions into HTTP responses.
        /// </remarks>
        /// <param name="reviewDTO">Review payload</param>
        /// <returns>Success message or error details</returns>
        [HttpPost("submitReview")]
        public async Task<IActionResult> SubmitReview([FromBody] ReviewRequestServiceDTO reviewDTO)
        {
            try
            {
                bool result = await _service.SubmitReviewAsync(reviewDTO);
                if (result)
                {
                    return Ok(new { message = "Review submitted successfully." });
                }
                else
                {
                    return BadRequest(new { message = "Failed to submit review." });
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
                return StatusCode(500, new { message = "An unexpected error occurred.", detail = ex.Message });
            }
        }

        /// <summary>
        /// Get reviews associated with a transport request.
        /// </summary>
        /// <param name="request_id">Transport request identifier</param>
        /// <returns>List of reviews or an error</returns>
        [HttpGet("reviews/{request_id}")]
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
                return StatusCode(500, new { message = "An unexpected error occurred.", detail = ex.Message });
            }
        }

        [HttpGet("average/driver/{driverId}")]
        public async Task<IActionResult> GetAverageDriverRating(int driverId)
        {
            var avg = await _service.GetAverageDriverRatingAsync(driverId);
            if (avg == null)
                return Ok(new { driverId, average = 0, message = "Sem avaliações" });
            return Ok(new { driverId, average = avg });
        }

        [HttpGet("average/company/{companyId}")]
        public async Task<IActionResult> GetAverageCompanyRating(int companyId)
        {
            var avg = await _service.GetAverageCompanyRatingAsync(companyId);
            if (avg == null)
                return Ok(new { companyId, average = 0, message = "Sem avaliações" });
            return Ok(new { companyId, average = avg });
        }



    }
}
