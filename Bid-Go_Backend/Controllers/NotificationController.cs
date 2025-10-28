using Bid_Go_Backend.Data.Models.Enums;
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
    [Route("api/notifications")]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationRepository _repo;

        public NotificationController(INotificationRepository repo)
        {
            _repo = repo;
        }

        // GET /api/notifications?userId=1&type=BidAccepted&from=2025-10-01&to=2025-10-28&order=asc
        [HttpGet]
        public async Task<IActionResult> GetNotifications(
            [FromQuery] int userId,
            [FromQuery] ENotificationType? type,
            [FromQuery] string order = "desc") // "asc" ou "desc"
        {
            var query = _repo.GetNotificationsAsync(userId, type);

            var notifications = await query;

            notifications = order.ToLower() switch
            {
                "asc" => notifications.OrderBy(n => n.TimeStamp).ToList(),
                _ => notifications.OrderByDescending(n => n.TimeStamp).ToList()
            };

            return Ok(notifications);
        }

    }
}

