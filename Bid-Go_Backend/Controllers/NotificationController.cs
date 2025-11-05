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

        // GET /api/notifications?userId=1&type=BidAccepted&order=desc
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
