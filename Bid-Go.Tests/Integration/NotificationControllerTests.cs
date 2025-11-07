using Bid_Go_Backend.Controllers;
using Bid_Go_Backend.Data;
using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.Enums;
using Bid_Go_Backend.Repositories.Notifications;
using Bid_Go_Backend.Services;
using Bid_Go_Backend.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Security.Claims;
using Xunit;

namespace Bid_Go.Tests.Integration
{
    /// <summary>
    /// Integration tests for notification retrieval and broadcast behavior.
    /// </summary>
    public class NotificationControllerTests
    {
        private static (NotificationController controller, BidGoDbContext db) BuildAs(int userId)
        {
            var options = new DbContextOptionsBuilder<BidGoDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            var db = new BidGoDbContext(options);

            var repo = new NotificationRepository(db);
            var hub = new Mock<IHubContext<NotificationHub>>();
            var clients = new Mock<IHubClients>();
            var clientProxy = new Mock<IClientProxy>();

            clients.Setup(c => c.User(It.IsAny<string>())).Returns(clientProxy.Object);
            hub.Setup(h => h.Clients).Returns(clients.Object);

            var service = new NotificationService(repo, hub.Object);
            var controller = new NotificationController(service);

            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim("userId", userId.ToString()),
                new Claim("userType", "Driver")
            }, "TestAuth"));

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };

            return (controller, db);
        }

        [Fact]
        public async Task GetNotifications_ReturnsOk_WithItems_FilteredAndOrdered()
        {
            // Arrange
            var (controller, db) = BuildAs(userId: 5);
            db.Notifications.AddRange(
                new Notification { UserId = 5, Context = "A", Type = ENotificationType.Accepted, TimeStamp = DateTime.UtcNow.AddMinutes(-2) },
                new Notification { UserId = 5, Context = "B", Type = ENotificationType.Rejected, TimeStamp = DateTime.UtcNow.AddMinutes(-1) },
                new Notification { UserId = 5, Context = "C", Type = ENotificationType.New_message, TimeStamp = DateTime.UtcNow }
            );
            await db.SaveChangesAsync();

            // Act
            var result = await controller.GetNotifications(userId: 5, type: null, order: "desc");

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            var list = Assert.IsType<List<Notification>>(ok.Value);
            Assert.Equal(3, list.Count);
            Assert.True(list[0].TimeStamp >= list[1].TimeStamp);
        }

        [Fact]
        public async Task CreateAndSend_CreatesNotification_AndCallsHub()
        {
            var (controller, db) = BuildAs(userId: 6);

            var repo = new NotificationRepository(db);
            var hub = new Mock<IHubContext<NotificationHub>>();
            var clients = new Mock<IHubClients>();
            var clientProxy = new Mock<IClientProxy>();

            clients.Setup(c => c.User(It.IsAny<string>())).Returns(clientProxy.Object);
            hub.Setup(h => h.Clients).Returns(clients.Object);

            var service = new NotificationService(repo, hub.Object);

            var created = await service.CreateAndSendAsync(6, "CTX", ENotificationType.Accepted, bidId: 1, transportRequestId: 2);

            Assert.True(created.NotificationId > 0);

            var fromDb = await db.Notifications.FindAsync(created.NotificationId);
            Assert.NotNull(fromDb);

            clientProxy.Verify(c => c.SendCoreAsync("ReceiveNotification", It.IsAny<object?[]>(), default), Times.Once);
        }
    }
}
