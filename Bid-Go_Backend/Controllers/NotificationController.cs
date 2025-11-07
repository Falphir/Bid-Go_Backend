using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.DTOs;
using Bid_Go_Backend.Data.Models.Enums;
using Bid_Go_Backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Bid_Go_Backend.Controllers
{
    [ApiController]
    [Route("api/notifications")]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _service;

        public NotificationController(INotificationService service)
        {
            _service = service;
        }

        /// <summary>
        /// Retrieve notifications for a user with optional filtering by type and ordering.
        /// </summary>
        /// <remarks>
        /// The endpoint supports optional query parameters for notification type and ordering. Paging is recommended for large result sets but not implemented here.
        /// </remarks>
        /// <param name="userId">User identifier to retrieve notifications for.</param>
        /// <param name="type">Optional notification type filter.</param>
        /// <param name="order">Sort order ("asc" or "desc"). Default is "desc".</param>
        /// <returns>List of notifications matching the filters.</returns>
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetNotifications(
            [FromQuery] int userId,
            [FromQuery] ENotificationType? type,
            [FromQuery] string order = "desc")
        {
            var notifications = await _service.GetNotificationsAsync(userId, type, order);
            return Ok(notifications);
        }
    }
}
