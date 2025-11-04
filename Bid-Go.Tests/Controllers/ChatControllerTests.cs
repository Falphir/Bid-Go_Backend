using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.DTOs;
using Bid_Go_Backend.Data.Models.Enums;
using Bid_Go_Backend.Data.Repositories.Interfaces;
using Bid_Go_Backend.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace Bid_Go.Tests.Controllers
{
    public class ChatControllerTests
    {
        private readonly Mock<IChatRepository> _chatRepo;
        private readonly Mock<ITransportRequestRepository> _requestRepo;
        private readonly ChatService _service;

        public ChatControllerTests()
        {
            _chatRepo = new Mock<IChatRepository>();
            _requestRepo = new Mock<ITransportRequestRepository>();
            _service = new ChatService(_chatRepo.Object, _requestRepo.Object);
        }

        private static ClaimsPrincipal MakeUser(int id, string type)
        {
            var claims = new[]
            {
                new Claim("userId", id.ToString()),
                new Claim("userType", type)
            };
            return new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
        }

        [Fact]
        public async Task GetChat_ShouldReturn404_WhenChatNotFound()
        {
            _chatRepo.Setup(r => r.GetChatByRequestIdAsync(9)).ReturnsAsync((Chats)null);

            var (status, body) = await _service.GetChat(9, MakeUser(1, "Driver"));

            Assert.Equal(404, status);
            var msg = body.GetType().GetProperty("message")!.GetValue(body) as string;
            Assert.Equal("Chat não encontrado.", msg);
        }

        [Fact]
        public async Task GetChat_ShouldReturn403_WhenNoAccess()
        {
            var chat = new Chats { ChatId = 1, TransportRequestId = 5, Status = EChatStatus.Active, Messages = new List<Message>() };
            var request = new TransportRequest { TransportRequestId = 5, CompanyId = 99, Bids = new List<Bid> { new Bid { Status = EBidStatus.Accepted, DriverId = 2 } } };

            _chatRepo.Setup(r => r.GetChatByRequestIdAsync(5)).ReturnsAsync(chat);
            _chatRepo.Setup(r => r.GetChatByIdAsync(chat.ChatId)).ReturnsAsync(chat);
            _requestRepo.Setup(r => r.GetRequestWithBidsByIdAsync(request.TransportRequestId)).ReturnsAsync(request);

            var (status, body) = await _service.GetChat(5, MakeUser(1, "Driver"));

            Assert.Equal(403, status);
        }

        [Fact]
        public async Task GetChat_ShouldReturn200_WithDTO()
        {
            var chat = new Chats
            {
                ChatId = 1,
                TransportRequestId = 5,
                Status = EChatStatus.Active,
                Messages = new List<Message>
                {
                    new Message { Context = "Hi", DriverId = 1, CompanyId = 2, TimeStamp = DateTime.UtcNow }
                }
            };
            var request = new TransportRequest { TransportRequestId = 5, CompanyId = 2, Bids = new List<Bid> { new Bid { Status = EBidStatus.Accepted, DriverId = 1 } } };

            _chatRepo.Setup(r => r.GetChatByRequestIdAsync(5)).ReturnsAsync(chat);
            _chatRepo.Setup(r => r.GetChatByIdAsync(chat.ChatId)).ReturnsAsync(chat);
            _requestRepo.Setup(r => r.GetRequestWithBidsByIdAsync(request.TransportRequestId)).ReturnsAsync(request);

            var (status, body) = await _service.GetChat(5, MakeUser(1, "Driver"));

            Assert.Equal(200, status);
            Assert.Equal(1, (int)body.GetType().GetProperty("ChatId")!.GetValue(body));
        }

        [Fact]
        public async Task SendMessage_ShouldReturn400_WhenRequestCanceled()
        {
            var chat = new Chats { ChatId = 1, TransportRequestId = 5, Status = EChatStatus.Active };
            var request = new TransportRequest { TransportRequestId = 5, CompanyId = 2, Status = ERequestStatus.Canceled };

            _chatRepo.Setup(r => r.GetChatByIdAsync(chat.ChatId)).ReturnsAsync(chat);
            _requestRepo.Setup(r => r.GetByIdAsync(chat.TransportRequestId)).ReturnsAsync(request);

            _requestRepo.Setup(r => r.GetRequestWithBidsByIdAsync(chat.TransportRequestId))
                .ReturnsAsync(new TransportRequest
                {
                    TransportRequestId = 5,
                    CompanyId = 2,
                    Bids = new List<Bid> { new Bid { Status = EBidStatus.Accepted, DriverId = 1 } }
                });

            var dto = new MessageDTO { Context = "Test" };
            var (status, body) = await _service.SendMessage(1, dto, MakeUser(1, "Driver"));

            Assert.Equal(400, status);
            var msg = body.GetType().GetProperty("message")!.GetValue(body) as string;
            Assert.Equal("Não é possível enviar mensagens neste chat, pois o pedido foi cancelado.", msg);
        }

        [Fact]
        public async Task SendMessage_ShouldReturn200_WhenOk()
        {
            var chatId = 1;
            var chat = new Chats { ChatId = chatId, TransportRequestId = 5, Status = EChatStatus.Active };
            var request = new TransportRequest
            {
                TransportRequestId = 5,
                CompanyId = 2,
                Status = ERequestStatus.Active,
                Bids = new List<Bid> { new Bid { Status = EBidStatus.Accepted, DriverId = 1 } }
            };

            _chatRepo.Setup(r => r.GetChatByIdAsync(chatId)).ReturnsAsync(chat);
            _requestRepo.Setup(r => r.GetByIdAsync(request.TransportRequestId)).ReturnsAsync(request);
            _chatRepo.Setup(r => r.AddMessageAsync(It.IsAny<Message>()))
                .ReturnsAsync((Message m) => { m.TimeStamp = DateTime.UtcNow; return m; });

            // For access check
            _requestRepo.Setup(r => r.GetRequestWithBidsByIdAsync(request.TransportRequestId)).ReturnsAsync(request);

            var dto = new MessageDTO { Context = "Hello" };
            var (status, body) = await _service.SendMessage(chatId, dto, MakeUser(1, "Driver"));

            Assert.Equal(200, status);
            Assert.Equal("Hello", (string)body.GetType().GetProperty("Context")!.GetValue(body));
        }

        [Fact]
        public async Task GetMessages_ShouldReturn404_WhenEmpty()
        {
            var chatId = 7;
            var chat = new Chats { ChatId = chatId, TransportRequestId = 3 };
            var request = new TransportRequest { TransportRequestId = 3, CompanyId = 2, Bids = new List<Bid> { new Bid { Status = EBidStatus.Accepted, DriverId = 1 } } };

            _chatRepo.Setup(r => r.GetChatByIdAsync(chatId)).ReturnsAsync(chat);
            _requestRepo.Setup(r => r.GetRequestWithBidsByIdAsync(request.TransportRequestId)).ReturnsAsync(request);
            _chatRepo.Setup(r => r.GetMessagesAsync(chatId)).ReturnsAsync(new List<ChatMessageDTO>());

            var (status, body) = await _service.GetMessages(chatId, MakeUser(1, "Driver"));

            Assert.Equal(404, status);
        }

        [Fact]
        public async Task CreateChatFromAcceptedBid_ShouldReturnOk_WhenChatCreated()
        {
            var transportRequestId = 10;
            _chatRepo.Setup(r => r.GetChatByTransportRequestIdAsync(transportRequestId)).ReturnsAsync((Chats)null);
            _chatRepo.Setup(r => r.GetAcceptedBidByRequestIdAsync(transportRequestId)).ReturnsAsync(new Bid { DriverId = 3, Status = EBidStatus.Accepted });
            _chatRepo.Setup(r => r.GetTransportRequestByIdAsync(transportRequestId)).ReturnsAsync(new TransportRequest { TransportRequestId = transportRequestId });
            _chatRepo.Setup(r => r.AddChatAsync(It.IsAny<Chats>())).ReturnsAsync((Chats c) => { c.ChatId = 123; return c; });

            var (status, body) = await _service.CreateChatFromAcceptedBid(transportRequestId);

            Assert.Equal(200, status);
            Assert.Equal(transportRequestId, (int)body.GetType().GetProperty("TransportRequestId")!.GetValue(body));
        }
    }
}
