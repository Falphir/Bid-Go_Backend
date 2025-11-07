using Bid_Go_Backend.Controllers;
using Bid_Go_Backend.Data;
using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.DTOs;
using Bid_Go_Backend.Data.Models.Enums;
using Bid_Go_Backend.Repositories.Chat;
using Bid_Go_Backend.Repositories.Interfaces;
using Bid_Go_Backend.Repositories.Transport_Request;
using Bid_Go_Backend.Services;
using Bid_Go_Backend.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Xunit;

namespace Bid_Go.Tests.Integration.Controllers
{
    /// <summary>
    /// Integration tests for ChatController validating access control, message flow and chat creation.
    /// </summary>
    public class ChatControllerTests
    {
        private static (ChatController controller, BidGoDbContext db, TestNotificationService notifications) BuildAsRole(string role, int userId)
        {
            var options = new DbContextOptionsBuilder<BidGoDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            var db = new BidGoDbContext(options);
            var chatRepo = new ChatRepository(db, notificationRepo: null!); // ctor exige, mas não usa no teste
            var reqRepo = new TransportRequestRepository(db);
            var notifications = new TestNotificationService();
            var chatService = new ChatService(chatRepo, reqRepo, notifications);
            var authzService = new AuthorizationService(new Bid_Go_Backend.Repositories.Authorization.AuthorizationRepository(db));
            var controller = new ChatController(chatService, authzService);

            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim("userId", userId.ToString()),
                new Claim("userType", role)
            }, "TestAuth"));

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };

            return (controller, db, notifications);
        }

        private static (TransportRequest tr, Chats chat, Driver driver, Company company, Bid acceptedBid) SeedAccepted(BidGoDbContext db)
        {
            var company = new Company
            {
                Name = "C",
                CompanyName = "CC",
                Address = "A",
                Email = "c@c.com",
                Password = "x",
                PhoneNumber = 900000000,
                NIF = 111111111
            };
            var driver = new Driver
            {
                Name = "D",
                Email = "d@x.com",
                Password = "x",
                PhoneNumber = 911111111,
                NIF = 123456789
            };

            db.Companies.Add(company);
            db.Drivers.Add(driver);

            // Persistir imediatamente para garantir que os IDs sejam gerados
            db.SaveChanges();

            var tr = new TransportRequest
            {
                Company = company,
                Status = ERequestStatus.Active,
                Origin = "A",
                Destination = "B",
                Package = "Box",
                Weight = 1,
                Volume = 1,
                Length = 1,
                Width = 1,
                Height = 1,
                PickupDate = DateTime.UtcNow,
                DeliveryDate = DateTime.UtcNow.AddDays(2),
                Image = "img",
                MaxPrice = 100,
                BiddingStartDate = DateTime.UtcNow.AddDays(-1),
                BiddingEndDate = DateTime.UtcNow.AddDays(1),
                IsAutomaticSelectionEnabled = false
            };

            db.TransportRequests.Add(tr);
            db.SaveChanges();

            var acceptedBid = new Bid
            {
                TransportRequestId = tr.TransportRequestId,
                DriverId = driver.Id,
                Status = EBidStatus.Accepted,
                Value = 50,
                DeliveryDeadline = DateTime.UtcNow.AddDays(1)
            };

            db.Bids.Add(acceptedBid);
            db.SaveChanges();

            // Garantir que o AuthorizationService encontre o Driver através de SelectedBid
            tr.SelectedBidId = acceptedBid.BidId;
            db.TransportRequests.Update(tr);
            db.SaveChanges();

            var chat = new Chats { Status = EChatStatus.Active, TransportRequestId = tr.TransportRequestId };
            db.Chats.Add(chat);
            db.SaveChanges();

            // Recarregar/obter entidades do contexto para garantir que não sejam nulas e possuam os IDs corretos
            driver = db.Drivers.Find(driver.Id)!;
            company = db.Companies.Find(company.Id)!;
            tr = db.TransportRequests.Include(t => t.Company).First(t => t.TransportRequestId == tr.TransportRequestId);
            acceptedBid = db.Bids.Find(acceptedBid.BidId)!;
            chat = db.Chats.Find(chat.ChatId)!;

            return (tr, chat, driver, company, acceptedBid);
        }

        [Fact]
        public async Task GetChat_ReturnsChat_WhenUserHasAccess()
        {
            // Arrange
            var (controller, db, _) = BuildAsRole("Company", userId: 1);
            var (tr, chat, driver, company, _) = SeedAccepted(db);

            // Act
            var result = await controller.GetChat(tr.TransportRequestId);

            // Assert
            var ok = Assert.IsType<ObjectResult>(result);
            Assert.Equal(200, ok.StatusCode);

            var chatDto = Assert.IsType<ChatDTO>(ok.Value);
            Assert.Equal(chat.ChatId, chatDto.ChatId);
        }

        [Fact]
        public async Task GetMessages_ReturnsMessages_WhenUserHasAccess()
        {
            // Arrange
            var (controller, db, _) = BuildAsRole("Driver", userId: 1);
            var (tr, chat, driver, company, _) = SeedAccepted(db);

            // associar o driver correto ao token
            controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim("userId", driver.Id.ToString()),
                new Claim("userType", "Driver")
            }, "TestAuth"));

            db.Messages.Add(new Message { ChatId = chat.ChatId, Context = "Olá", DriverId = driver.Id, CompanyId = 0, TimeStamp = DateTime.UtcNow });
            db.Messages.Add(new Message { ChatId = chat.ChatId, Context = "Olá também", DriverId = 0, CompanyId = company.Id, TimeStamp = DateTime.UtcNow.AddMinutes(1) });

            await db.SaveChangesAsync();

            // Act
            var result = await controller.GetMessages(chat.ChatId);

            // Assert
            var ok = Assert.IsType<ObjectResult>(result);
            Assert.Equal(200, ok.StatusCode);

            var messages = Assert.IsAssignableFrom<IEnumerable<ChatMessageDTO>>(ok.Value);
            Assert.Equal(2, messages.Count());
        }

        [Fact]
        public async Task SendMessage_AsCompany_SendsAndNotifies()
        {
            // Arrange
            var (controller, db, notifications) = BuildAsRole("Company", userId: 2);
            var (tr, chat, driver, company, _) = SeedAccepted(db);

            controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim("userId", company.Id.ToString()),
                new Claim("userType", "Company")
            }, "TestAuth"));

            var dto = new MessageSentDTO { Context = "Mensagem para o driver" };

            // Act
            var result = await controller.SendMessage(chat.ChatId, dto);

            // Assert
            var ok = Assert.IsType<ObjectResult>(result);
            Assert.Equal(200, ok.StatusCode);

            var stored = await db.Messages.Where(m => m.ChatId == chat.ChatId).ToListAsync();
            Assert.Single(stored);
            Assert.Contains(notifications.Created, n => n.Type == ENotificationType.New_message && n.TransportRequestId == tr.TransportRequestId);
        }

        [Fact]
        public async Task CreateChatFromAcceptedBid_Creates_WhenNotExists()
        {
            // Arrange
            var (controller, db, _) = BuildAsRole("Company", userId: 3);
            var (tr, chat, driver, company, acceptedBid) = SeedAccepted(db);

            // remove chat existente
            db.Chats.Remove(chat);
            await db.SaveChangesAsync();

            // Act
            var result = await controller.CreateChatFromAcceptedBid(tr.TransportRequestId);

            // Assert
            var ok = Assert.IsType<ObjectResult>(result);
            Assert.Equal(200, ok.StatusCode);

            var view = Assert.IsType<ViewChatDTO>(ok.Value);
            Assert.True(view.ChatId > 0);
        }

        // notificação inline
        private sealed class TestNotificationService : INotificationService
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
                    NotificationId = Created.Count + 1,
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
                        NotificationId = Created.Count + 1,
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
}
