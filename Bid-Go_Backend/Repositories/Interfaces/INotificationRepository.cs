using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.Enums;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Bid_Go_Backend.Repositories.Interfaces
{
    public interface INotificationRepository
    {
        Task<Notification> CreateAsync(int userId, string context, ENotificationType type,
                                       int? bidId = null, int? transportRequestId = null);

        Task<List<Notification>> GetNotificationsAsync(int userId, ENotificationType? type = null, string order = "desc");

        Task MarkAsReadAsync(int notificationId);
        Task MarkAllAsReadAsync(int userId);

    }
}
