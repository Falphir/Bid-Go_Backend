using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.DTOs;
using Bid_Go_Backend.Data.Models.Enums;
using Bid_Go_Backend.Repositories.Interfaces;
using Bid_Go_Backend.Services;
using Bid_Go_Backend.Services.Interfaces;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace Bid_Go.Tests.Unit.Services
{

    /// <summary>
    /// Unit tests for ChatService verifying access control and message/chat flows.
    /// </summary>
    public class ChatServiceTests
    {
        private readonly Mock<IChatRepository> _chatRepoMock;
        private readonly Mock<ITransportRequestRepository> _requestRepoMock;
        private readonly Mock<INotificationService> _notificationServiceMock;
        private readonly ChatService _service;


        public ChatServiceTests()
        {
            _chatRepoMock = new Mock<IChatRepository>();
            _requestRepoMock = new Mock<ITransportRequestRepository>();
            _notificationServiceMock = new Mock<INotificationService>();
            _service = new ChatService(_chatRepoMock.Object, _requestRepoMock.Object, _notificationServiceMock.Object);
            ;
        }

        private ClaimsPrincipal CreateUser(int userId, string role)
        {
            var claims = new List<Claim>
        {
            new Claim("userId", userId.ToString()),
            new Claim("userType", role)
        };
            return new ClaimsPrincipal(new ClaimsIdentity(claims, "mock"));
        }

        [Fact]
        public async Task GetChat_ShouldReturn404_WhenChatNotFound()
        {
            // Arrange
            _chatRepoMock.Setup(r => r.GetChatByRequestIdAsync(1)).ReturnsAsync((Chats)null);

            // Act
            var result = await _service.GetChat(1, CreateUser(1, "Driver"));

            // Assert
            Assert.Equal(404, result.StatusCode);
            Assert.Contains("Chat não encontrado", result.Body.ToString());
        }

        [Fact]
        public async Task GetChat_ShouldReturn403_WhenUserNoAccess()
        {
            // Arrange
            var chat = new Chats { ChatId = 1, TransportRequestId = 1 };
            _chatRepoMock.Setup(r => r.GetChatByRequestIdAsync(1)).ReturnsAsync(chat);
            _requestRepoMock.Setup(r => r.GetRequestWithBidsByIdAsync(1))
                            .ReturnsAsync(new TransportRequest
                            {
                                TransportRequestId = 1,
                                CompanyId = 2,
                                Bids = new List<Bid> { new Bid { Status = EBidStatus.Accepted, DriverId = 3 } }
                            });

            // Act
            var result = await _service.GetChat(1, CreateUser(99, "Driver"));

            // Assert
            Assert.Equal(403, result.StatusCode);
            Assert.Contains("Acesso negado", result.Body.ToString());
        }

        [Fact]
        public async Task SendMessage_ShouldReturn403_WhenUserNoAccess()
        {
            // Arrange
            _chatRepoMock.Setup(r => r.GetChatByIdAsync(1))
                         .ReturnsAsync(new Chats { ChatId = 1, TransportRequestId = 1 });
            _requestRepoMock.Setup(r => r.GetRequestWithBidsByIdAsync(1))
                            .ReturnsAsync(new TransportRequest
                            {
                                TransportRequestId = 1,
                                CompanyId = 2,
                                Bids = new List<Bid> { new Bid { Status = EBidStatus.Accepted, DriverId = 3 } }
                            });

            // Act
            var result = await _service.SendMessage(1, new MessageSentDTO { Context = "Hi" }, CreateUser(99, "Driver"));

            // Assert
            Assert.Equal(403, result.StatusCode);
            Assert.Contains("Acesso negado", result.Body.ToString());
        }



        [Fact]
        public async Task CreateChatFromAcceptedBid_ShouldReturn400_WhenNoAcceptedBid()
        {
            // Arrange
            _chatRepoMock.Setup(r => r.GetChatByTransportRequestIdAsync(1))
                         .ReturnsAsync((Chats)null);

            _chatRepoMock.Setup(r => r.GetAcceptedBidByRequestIdAsync(1))
                         .ReturnsAsync((Bid)null);

            // Act
            var result = await _service.CreateChatFromAcceptedBid(1);

            // Assert
            Assert.Equal(400, result.StatusCode);
            Assert.Contains("Nenhuma bid aceite encontrada", result.Body.ToString());
        }

        [Fact]
        public async Task CreateChatFromAcceptedBid_ShouldReturn200_WhenChatCreated()
        {
            // Arrange
            _chatRepoMock.Setup(r => r.GetChatByTransportRequestIdAsync(1))
                         .ReturnsAsync((Chats)null);

            _chatRepoMock.Setup(r => r.GetAcceptedBidByRequestIdAsync(1))
                         .ReturnsAsync(new Bid { Status = EBidStatus.Accepted });

            _chatRepoMock.Setup(r => r.GetTransportRequestByIdAsync(1))
                         .ReturnsAsync(new TransportRequest { TransportRequestId = 1 });

            _chatRepoMock.Setup(r => r.AddChatAsync(It.IsAny<Chats>()))
                         .ReturnsAsync((Chats chat) =>
                         {
                             chat.ChatId = 123;
                             return chat;
                         });

            // Act
            var result = await _service.CreateChatFromAcceptedBid(1);

            // Assert
            Assert.Equal(200, result.StatusCode);

            var dto = Assert.IsType<ViewChatDTO>(result.Body);
            Assert.Equal(123, dto.ChatId);
        }

        [Fact]
        public async Task SendMessage_ShouldReturn200_WhenMessageSentByCompany_WithAcceptedBid()
        {
            // Arrange
            var chat = new Chats { ChatId = 1, TransportRequestId = 1, Status = EChatStatus.Active };
            var request = new TransportRequest
            {
                TransportRequestId = 1,
                Status = ERequestStatus.Active,
                CompanyId = 10,
                Bids = new List<Bid> { new Bid { Status = EBidStatus.Accepted, DriverId = 2 } }
            };

            _chatRepoMock.Setup(r => r.GetChatByIdAsync(1)).ReturnsAsync(chat);
            _requestRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(request);
            _requestRepoMock.Setup(r => r.GetRequestWithBidsByIdAsync(1)).ReturnsAsync(request);
            _chatRepoMock.Setup(r => r.AddMessageAsync(It.IsAny<Message>())).ReturnsAsync((Message m) => m);

            var user = CreateUser(10, "Company");

            // Act
            var result = await _service.SendMessage(1, new MessageSentDTO { Context = "Olá driver" }, user);

            // Assert
            Assert.Equal(200, result.StatusCode);
            var dto = Assert.IsType<MessageSentDTO>(result.Body);
            Assert.Equal("Olá driver", dto.Context);

        }


        [Fact]
        public async Task SendMessage_ShouldReturn403_WhenCompanyHasNoAcceptedBid()
        {
            // Arrange
            var chat = new Chats { ChatId = 1, TransportRequestId = 1, Status = EChatStatus.Active };
            var request = new TransportRequest
            {
                TransportRequestId = 1,
                Status = ERequestStatus.Active,
                CompanyId = 10,
                Bids = new List<Bid> { new Bid { Status = EBidStatus.Pendent, DriverId = 2 } } // nenhuma bid aceita
            };

            _chatRepoMock.Setup(r => r.GetChatByIdAsync(1)).ReturnsAsync(chat);
            _requestRepoMock.Setup(r => r.GetRequestWithBidsByIdAsync(1)).ReturnsAsync(request);

            var user = CreateUser(10, "Company");

            // Act
            var result = await _service.SendMessage(1, new MessageSentDTO { Context = "Oi" }, user);

            // Assert
            // Na prática, como não há bid aceita, o acesso falha → 403
            Assert.Equal(403, result.StatusCode);
            Assert.Contains("Acesso negado", result.Body.ToString());
        }


    }
}