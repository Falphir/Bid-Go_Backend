using Bid_Go_Backend.Data;
using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.Enums;
using Bid_Go_Backend.Repositories.Interface;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Bid_Go_Backend.Repositories
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly BidGoDbContext _ctx;

        public NotificationRepository(BidGoDbContext ctx)
        {
            _ctx = ctx;
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
    }
}