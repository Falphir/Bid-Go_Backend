using Bid_Go_Backend.Services.Interfaces;
using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.Enums;

namespace Bid_Go.Tests.Integration.Utils
{
 public class FakeNotificationService : INotificationService
 {
 public List<Notification> Created { get; } = new();

 public Task<List<Notification>> GetNotificationsAsync(int userId, ENotificationType? type = null, string order = "desc")
 {
 var q = Created.Where(n => n.UserId == userId);
 if (type.HasValue) q = q.Where(n => n.Type == type);
 return Task.FromResult(q.ToList());
 }

 public Task<Notification> CreateAndSendAsync(int userId, string context, ENotificationType type, int? bidId = null, int? transportRequestId = null)
 {
 var n = new Notification
 {
 NotificationId = Created.Count +1,
 UserId = userId,
 Context = context,
 Type = type,
 BidId = bidId,
 TransportRequestId = transportRequestId,
 TimeStamp = DateTime.UtcNow
 };
 Created.Add(n);
 return Task.FromResult(n);
 }

 public Task SendToMultipleUsersAsync(IEnumerable<int> userIds, string context, ENotificationType type)
 {
 foreach (var id in userIds)
 {
 Created.Add(new Notification
 {
 NotificationId = Created.Count +1,
 UserId = id,
 Context = context,
 Type = type,
 TimeStamp = DateTime.UtcNow
 });
 }
 return Task.CompletedTask;
 }
 }
}
