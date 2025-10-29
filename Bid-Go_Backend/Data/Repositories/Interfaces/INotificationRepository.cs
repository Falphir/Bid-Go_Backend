using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bid_Go_Backend.Data.Repositories.Interfaces
{

    public interface INotificationRepository
    {
        Task<Notification> CreateAsync(int userId, string context, ENotificationType type,
                                       int? bidId = null, int? transportRequestId = null);

        Task SendAsync(int userId, string context, ENotificationType type);

        Task<List<Notification>> GetNotificationsAsync(int userId, ENotificationType? type = null, string order = "desc");
    }

}
