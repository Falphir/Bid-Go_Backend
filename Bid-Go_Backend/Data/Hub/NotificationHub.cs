using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

public class NotificationHub : Hub
{
    // Enviar notificação a um utilizador específico
    public async Task SendNotificationToUser(int userId, string context, string type)
    {
        await Clients.User(userId.ToString())
                     .SendAsync("ReceiveNotification", new { context, type });
    }

    // Enviar notificação a vários utilizadores
    public async Task SendNotificationToUsers(int[] userIds, string context, string type)
    {
        foreach (var userId in userIds)
        {
            await Clients.User(userId.ToString())
                         .SendAsync("ReceiveNotification", new { context, type });
        }
    }
}
