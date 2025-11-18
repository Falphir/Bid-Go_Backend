using Bid_Go_Backend.Data;
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
        private readonly BidGoDbContext _ctx;
        public NotificationController(INotificationService service, BidGoDbContext ctx)
        {
            _service = service;
            _ctx = ctx;
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

        [Authorize]
        [HttpPatch("mark-read/{id}")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            int userId = int.Parse(User.FindFirst("userId")!.Value);

       
            var notif = await _ctx.Notifications.FindAsync(id);

            if (notif == null)
                return NotFound(new { message = "Notification not found." });

            if (notif.UserId != userId)
                return Forbid("You cannot modify another user's notification.");

            await _service.MarkAsReadAsync(id);

            return Ok(new { message = "Notification marked as read." });
        }


        [Authorize]
        [HttpPatch("mark-all-read")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            int userId = int.Parse(User.FindFirst("userId")!.Value);

            await _service.MarkAllAsReadAsync(userId);

            return Ok(new { message = "All notifications marked as read." });
        }

    }
}
