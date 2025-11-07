using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.Enums;
using Bid_Go_Backend.Repositories.Interfaces;
using Bid_Go_Backend.Services.Interfaces;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Bid_Go_Backend.Services
{
    /// <summary>
    /// Service for creating notifications and delivering them via SignalR.
    /// </summary>
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _repo;
        private readonly IHubContext<NotificationHub> _hub;

        public NotificationService(INotificationRepository repo, IHubContext<NotificationHub> hub)
        {
            _repo = repo;
            _hub = hub;
        }

        /// <summary>
        /// Retrieve notifications for a user with optional type and order filters.
        /// </summary>
        /// <param name="userId">The ID of the user for whom to retrieve notifications.</param>
        /// <param name="type">Optional. The type of notifications to retrieve.</param>
        /// <param name="order">Optional. The order in which to sort the notifications. Default is descending.</param>
        /// <returns>A list of notifications.</returns>
        public async Task<List<Notification>> GetNotificationsAsync(int userId, ENotificationType? type = null, string order = "desc")
        {
            return await _repo.GetNotificationsAsync(userId, type, order);
        }

        /// <summary>
        /// Create a notification and broadcast it to a specific user via SignalR.
        /// </summary>
        /// <param name="userId">The ID of the user to whom the notification will be sent.</param>
        /// <param name="context">The context or message of the notification.</param>
        /// <param name="type">The type of notification.</param>
        /// <param name="bidId">Optional. The ID of the bid associated with the notification.</param>
        /// <param name="transportRequestId">Optional. The ID of the transport request associated with the notification.</param>
        /// <returns>The created notification.</returns>
        public async Task<Notification> CreateAndSendAsync(int userId, string context, ENotificationType type,
                                                           int? bidId = null, int? transportRequestId = null)
        {
            // Business rules could be added here (e.g., user activity checks)
            var notification = await _repo.CreateAsync(userId, context, type, bidId, transportRequestId);

            await _hub.Clients.User(userId.ToString())
                .SendAsync("ReceiveNotification", new { context, type, notification.TimeStamp });

            return notification;
        }

        /// <summary>
        /// Broadcast a notification to multiple users via SignalR only (no persistence).
        /// </summary>
        /// <param name="userIds">The IDs of the users to whom the notification will be sent.</param>
        /// <param name="context">The context or message of the notification.</param>
        /// <param name="type">The type of notification.</param>
        public async Task SendToMultipleUsersAsync(IEnumerable<int> userIds, string context, ENotificationType type)
        {
            foreach (var userId in userIds)
            {
                await _hub.Clients.User(userId.ToString())
                    .SendAsync("ReceiveNotification", new { context, type, TimeStamp = DateTime.UtcNow });
            }
        }
    }

}
