using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.Enums;
using Bid_Go_Backend.Repositories.Interface;
using Bid_Go_Backend.Services.Interfaces;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Bid_Go_Backend.Services
{
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _repo;
        private readonly IHubContext<NotificationHub> _hub;

        public NotificationService(INotificationRepository repo, IHubContext<NotificationHub> hub)
        {
            _repo = repo;
            _hub = hub;
        }

        public async Task<List<Notification>> GetNotificationsAsync(int userId, ENotificationType? type = null, string order = "desc")
        {
            return await _repo.GetNotificationsAsync(userId, type, order);
        }

        public async Task<Notification> CreateAndSendAsync(int userId, string context, ENotificationType type,
                                                           int? bidId = null, int? transportRequestId = null)
        {
            // Regras de negócio poderiam ir aqui
            // Exemplo: verificar se o utilizador existe ou se está ativo

            var notification = await _repo.CreateAsync(userId, context, type, bidId, transportRequestId);

            // Enviar via SignalR (se aplicável)
            await _hub.Clients.User(userId.ToString())
                .SendAsync("ReceiveNotification", new { context, type, notification.TimeStamp });

            return notification;
        }

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
