using Bid_Go_Backend.Data;
using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.Enums;
using Bid_Go_Backend.Data.Repositories.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Bid_Go_Backend.Data.Repositories.Notifications
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly BidGoDbContext _ctx;
        private readonly IHubContext<NotificationHub> _hub;

        public NotificationRepository(BidGoDbContext ctx, IHubContext<NotificationHub> hub)
        {
            _ctx = ctx;
            _hub = hub;
        }

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

        public async Task SendAsync(int userId, string context, ENotificationType type)
        {
            await _hub.Clients.User(userId.ToString())
                             .SendAsync("ReceiveNotification", new { context, type });
        }

        public async Task<List<Notification>> GetNotificationsAsync(int userId, ENotificationType? type = null)
        {
            var query = _ctx.Notifications.AsQueryable();
            query = query.Where(n => n.UserId == userId);

            if (type.HasValue)
                query = query.Where(n => n.Type == type.Value);

            return await query.OrderByDescending(n => n.TimeStamp).ToListAsync();
        }
    }
}
