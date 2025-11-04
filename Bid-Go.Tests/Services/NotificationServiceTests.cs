using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.Enums;
using Bid_Go_Backend.Repositories.Interface;
using Bid_Go_Backend.Services;
using Bid_Go_Backend.Services.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Bid_Go.Tests.Services
{
    public class NotificationServiceTests
    {
        private readonly Mock<INotificationRepository> _mockRepo;
        private readonly Mock<IHubContext<NotificationHub>> _mockHubContext;
        private readonly Mock<IClientProxy> _mockClientProxy;
        private readonly Mock<IHubClients> _mockClients;
        private readonly NotificationService _service;

        public NotificationServiceTests()
        {
            _mockRepo = new Mock<INotificationRepository>();
            _mockHubContext = new Mock<IHubContext<NotificationHub>>();
            _mockClients = new Mock<IHubClients>();
            _mockClientProxy = new Mock<IClientProxy>();

            // Setup do Hub
            _mockClients.Setup(c => c.User(It.IsAny<string>())).Returns(_mockClientProxy.Object);
            _mockHubContext.Setup(h => h.Clients).Returns(_mockClients.Object);

            _service = new NotificationService(_mockRepo.Object, _mockHubContext.Object);
        }

        // ===========================================================
        // GetNotificationsAsync
        // ===========================================================
        [Fact]
        public async Task GetNotificationsAsync_ShouldReturnNotifications()
        {
            // Arrange
            int userId = 1;
            var expected = new List<Notification>
            {
                new Notification { NotificationId = 1, UserId = userId, Context = "Test 1", Type = ENotificationType.Accepted },
                new Notification { NotificationId = 2, UserId = userId, Context = "Test 2", Type = ENotificationType.Rejected }
            };

            _mockRepo
                .Setup(r => r.GetNotificationsAsync(userId, null, "desc"))
                .ReturnsAsync(expected);

            // Act
            var result = await _service.GetNotificationsAsync(userId);

            // Assert
            Assert.Equal(expected, result);
            _mockRepo.Verify(r => r.GetNotificationsAsync(userId, null, "desc"), Times.Once);
        }

        // ===========================================================
        // CreateAndSendAsync
        // ===========================================================
        [Fact]
        public async Task CreateAndSendAsync_ShouldCreateAndSendNotification()
        {
            // Arrange
            int userId = 1;
            var type = ENotificationType.Accepted;
            var notification = new Notification
            {
                NotificationId = 10,
                UserId = userId,
                Context = "Bid accepted",
                Type = type,
                TimeStamp = DateTime.UtcNow
            };

            _mockRepo
                .Setup(r => r.CreateAsync(userId, "Bid accepted", type, null, null))
                .ReturnsAsync(notification);

            // Act
            var result = await _service.CreateAndSendAsync(userId, "Bid accepted", type);

            // Assert
            Assert.Equal(notification, result);
            _mockRepo.Verify(r => r.CreateAsync(userId, "Bid accepted", type, null, null), Times.Once);
            _mockClients.Verify(c => c.User(userId.ToString()), Times.Once);
            _mockClientProxy.Verify(c => c.SendCoreAsync(
                "ReceiveNotification",
                It.IsAny<object[]>(),
                default), Times.Once);
        }

        // ===========================================================
        // SendToMultipleUsersAsync
        // ===========================================================
        [Fact]
        public async Task SendToMultipleUsersAsync_ShouldSendToAllUsers()
        {
            // Arrange
            var userIds = new List<int> { 1, 2, 3 };
            string context = "System message";
            var type = ENotificationType.New_message;

            // Act
            await _service.SendToMultipleUsersAsync(userIds, context, type);

            // Assert
            foreach (var id in userIds)
            {
                _mockClients.Verify(c => c.User(id.ToString()), Times.Once);
            }

            _mockClientProxy.Verify(c => c.SendCoreAsync(
                "ReceiveNotification",
                It.IsAny<object[]>(),
                default), Times.Exactly(userIds.Count));
        }
    }
}
