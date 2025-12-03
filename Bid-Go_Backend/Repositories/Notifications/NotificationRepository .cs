using Bid_Go_Backend.Data;
using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.Enums;
using Bid_Go_Backend.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Bid_Go_Backend.Repositories.Notifications
{
    /// <summary>
    /// Repository for creating and querying notifications.
    /// </summary>
    public class NotificationRepository : INotificationRepository
    {
        private readonly BidGoDbContext _ctx;

        public NotificationRepository(BidGoDbContext ctx)
        {
            _ctx = ctx;
        }

        /// <summary>
        /// Create and persist a notification.
        /// </summary>
        /// <param name="userId">Target user identifier.</param>
        /// <param name="context">Notification message/context.</param>
        /// <param name="type">Notification type.</param>
        /// <param name="bidId">Optional related bid identifier.</param>
        /// <param name="transportRequestId">Optional related transport request identifier.</param>
        /// <returns>The created notification.</returns>
        public async Task<Notification> CreateAsync(int userId, string context, ENotificationType type,
                                                    int? bidId = null, int? transportRequestId = null)
        {
            var notification = new Notification
            {
                UserId = userId,
                Context = context,
                Type = type,
                TimeStamp = DateTime.UtcNow,
                BidId = bidId,
                TransportRequestId = transportRequestId
            };

            _ctx.Notifications.Add(notification);
            await _ctx.SaveChangesAsync();
            return notification;
        }

        /// <summary>
        /// Get notifications filtered by user and optionally by type and order.
        /// </summary>
        /// <param name="userId">User identifier.</param>
        /// <param name="type">Optional filter by type.</param>
        /// <param name="order">Ordering: "asc" or "desc" (default).</param>
        /// <returns>List of notifications.</returns>
        public async Task<List<Notification>> GetNotificationsAsync(int userId, ENotificationType? type = null, string order = "desc")
        {
            var query = _ctx.Notifications.AsQueryable()
                .Where(n => n.UserId == userId);

            if (type.HasValue)
                query = query.Where(n => n.Type == type.Value);

            query = order.ToLower() == "asc"
                ? query.OrderBy(n => n.TimeStamp)
                : query.OrderByDescending(n => n.TimeStamp);

            return await query.ToListAsync();
        }

        public async Task MarkAsReadAsync(int notificationId)
        {
            var notification = await _ctx.Notifications.FindAsync(notificationId);
            if (notification == null) return;

            notification.IsRead = true;
            await _ctx.SaveChangesAsync();
        }

        public async Task MarkAllAsReadAsync(int userId)
        {
            var notifications = await _ctx.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            foreach (var n in notifications)
                n.IsRead = true;

            await _ctx.SaveChangesAsync();
        }

    }
}