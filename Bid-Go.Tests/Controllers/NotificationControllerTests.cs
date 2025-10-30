using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bid_Go_Backend.Controllers;
using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.Enums;
using Bid_Go_Backend.Data.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Bid_Go.Tests.Controllers
{
    public class NotificationControllerTests
    {
        private readonly Mock<INotificationRepository> _mockRepo;
        private readonly NotificationController _controller;

        public NotificationControllerTests()
        {
            _mockRepo = new Mock<INotificationRepository>();
            _controller = new NotificationController(_mockRepo.Object);
        }

        [Fact]
        public async Task GetNotifications_ShouldReturnOkWithNotifications_WhenNotificationsExist()
        {
            // Arrange
            var notifications = new List<Notification>
            {
                new Notification
                {
                    NotificationId = 1,
                    UserId = 1,
                    Context = "Pedido aceite",
                    Type = ENotificationType.Accepted,
                    TimeStamp = DateTime.UtcNow
                },
                new Notification
                {
                    NotificationId = 2,
                    UserId = 1,
                    Context = "Pagamento confirmado",
                    Type = ENotificationType.Confirmed_Payment,
                    TimeStamp = DateTime.UtcNow.AddMinutes(-10)
                }
            };

            _mockRepo.Setup(r => r.GetNotificationsAsync(1, null, "desc"))
                     .ReturnsAsync(notifications);

            // Act
            var result = await _controller.GetNotifications(1, null, "desc");

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            var returned = Assert.IsType<List<Notification>>(ok.Value);
            Assert.Equal(2, returned.Count);
            Assert.Equal(notifications, returned);
        }

        [Fact]
        public async Task GetNotifications_ShouldReturnOkWithEmptyList_WhenNoNotifications()
        {
            // Arrange
            _mockRepo.Setup(r => r.GetNotificationsAsync(2, null, "asc"))
                     .ReturnsAsync(new List<Notification>());

            // Act
            var result = await _controller.GetNotifications(2, null, "asc");

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            var returned = Assert.IsType<List<Notification>>(ok.Value);
            Assert.Empty(returned);
        }

        [Fact]
        public async Task GetNotifications_ShouldReturnOk_WhenTypeAndOrderProvided()
        {
            // Arrange
            var notifications = new List<Notification>
            {
                new Notification
                {
                    NotificationId = 3,
                    UserId = 3,
                    Context = "Licitação rejeitada",
                    Type = ENotificationType.Rejected,
                    TimeStamp = DateTime.UtcNow
                }
            };

            _mockRepo.Setup(r => r.GetNotificationsAsync(3, ENotificationType.Rejected, "asc"))
                     .ReturnsAsync(notifications);

            // Act
            var result = await _controller.GetNotifications(3, ENotificationType.Rejected, "asc");

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            var returned = Assert.IsType<List<Notification>>(ok.Value);
            Assert.Single(returned);
            Assert.Equal(ENotificationType.Rejected, returned[0].Type);
        }
    }
}
