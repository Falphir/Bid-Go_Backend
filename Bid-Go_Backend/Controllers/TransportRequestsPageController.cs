using Bid_Go_Backend.Data.Models.DTOs;
using Bid_Go_Backend.Services.Transport_Request;
using Microsoft.AspNetCore.Mvc;

namespace Bid_Go_Backend.Controllers
{
    [ApiController]
    [Route("api/pageTransports")]
    public class TransportRequestsPageController : ControllerBase
    {
        private readonly ITransportRequestsPageService _service;

        public TransportRequestsPageController(ITransportRequestsPageService service)
        {
            _service = service;
        }

        /// <summary>
        /// Get active transport requests with optional filters.
        /// </summary>
        /// <remarks>
        /// Supports filtering by origin, destination, delivery date and price ordering. Consider adding paging for large datasets.
        /// </remarks>
        /// <returns>List of transport requests or a message when none are available.</returns>
        // GET: api/PageRequests
        [HttpGet("filters")]
        public async Task<ActionResult<IEnumerable<TransportRequestsPageDTO>>> GetActive(
            [FromQuery] string? origin,
            [FromQuery] string? destination,
            [FromQuery] DateTime? deliveryDate,
            [FromQuery] string? priceOrder)
        {
            try
            {
                var requests = await _service.GetActiveAsync(origin, destination, deliveryDate, priceOrder);

                if (!requests.Any())
                    return Ok(new { message = "No active transport requests at the moment." });

                var dtoList = requests.Select(tr => new TransportRequestsPageDTO
                {
                    id = tr.TransportRequestId,
                    Origin = tr.Origin,
                    Destination = tr.Destination,
                    Package = tr.Package,
                    PickupDate = tr.PickupDate,
                    DeliveryDate = tr.DeliveryDate,
                    BiddingStartDate = tr.BiddingStartDate,
                    BiddingEndDate = tr.BiddingEndDate,
                    Image = tr.Image,
                    MaxPrice = tr.MaxPrice
                });

                return Ok(dtoList);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Unexpected error: " + ex.Message });
            }
        }

        /// <summary>
        /// Get a single transport request by id for the public page view.
        /// </summary>
        /// <param name="id">Transport request identifier.</param>
        /// <returns>Transport request details or 404.</returns>
        // GET: api/PageRequests/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<TransportRequestResponseDTO>> GetById(int id)
        {
            try
            {
                var target = await _service.GetByIdAsync(id);

                if (target == null)
                    return NotFound(new { message = "Transport request does not exist." });

                var responseDto = new TransportRequestResponseDTO
                {
                    id = target.TransportRequestId,
                    Origin = target.Origin,
                    Destination = target.Destination,
                    Package = target.Package,
                    PickupDate = target.PickupDate,
                    DeliveryDate = target.DeliveryDate,
                    Weight = target.Weight,
                    Volume = target.Volume,
                    Length = target.Length,
                    Width = target.Width,
                    Height = target.Height,
                    MaxPrice = target.MaxPrice,
                    Image = target.Image
                };

                return Ok(responseDto);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Unexpected error: " + ex.Message });
            }
        }
    }
}
