using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.Enums;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Bid_Go_Backend.Services.Interfaces
{
    public interface INotificationService
    {
        Task<List<Notification>> GetNotificationsAsync(int userId, ENotificationType? type = null, string order = "desc");
        Task<Notification> CreateAndSendAsync(int userId, string context, ENotificationType type,
                                                           int? bidId = null, int? transportRequestId = null);

        Task SendToMultipleUsersAsync(IEnumerable<int> userIds, string context, ENotificationType type);
    }
}
