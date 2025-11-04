using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.DTOs;
using Bid_Go_Backend.Data.Models.Enums;
using Bid_Go_Backend.Data.Repositories.Interfaces;
using Bid_Go_Backend.Services.Chat;
using Moq;
using Xunit;

public class ChatServiceTests
{
    private readonly Mock<IChatRepository> _chatRepoMock;
    private readonly Mock<ITransportRequestRepository> _requestRepoMock;
    private readonly ChatService _service;

    public ChatServiceTests()
    {
        _chatRepoMock = new Mock<IChatRepository>();
        _requestRepoMock = new Mock<ITransportRequestRepository>();
        _service = new ChatService(_chatRepoMock.Object, _requestRepoMock.Object);
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
        _chatRepoMock.Setup(r => r.GetChatByRequestIdAsync(1))
                     .ReturnsAsync((Chats)null);

        var result = await _service.GetChat(1, CreateUser(1, "Driver"));

        Assert.Equal(404, result.StatusCode);
        Assert.Contains("Chat não encontrado", result.Body.ToString());
    }

    [Fact]
    public async Task SendMessage_ShouldReturn403_WhenUserNoAccess()
    {
        _chatRepoMock.Setup(r => r.GetChatByIdAsync(1))
                     .ReturnsAsync(new Chats { ChatId = 1, TransportRequestId = 1 });
        _requestRepoMock.Setup(r => r.GetRequestWithBidsByIdAsync(1))
                        .ReturnsAsync(new TransportRequest
                        {
                            TransportRequestId = 1,
                            CompanyId = 2,
                            Bids = new List<Bid> { new Bid { Status = EBidStatus.Accepted, DriverId = 3 } }
                        });

        var result = await _service.SendMessage(1, new MessageDTO { Context = "Hi" }, CreateUser(99, "Driver"));

        Assert.Equal(403, result.StatusCode);
        Assert.Contains("Acesso negado", result.Body.ToString());
    }

    [Fact]
    public async Task SendMessage_ShouldReturn400_WhenRequestCanceled()
    {
        var chat = new Chats { ChatId = 1, TransportRequestId = 1, Status = EChatStatus.Active };
        var request = new TransportRequest
        {
            TransportRequestId = 1,
            Status = ERequestStatus.Canceled,
            CompanyId = 1,
            Bids = new List<Bid> { new Bid { Status = EBidStatus.Accepted, DriverId = 2 } }
        };

        _chatRepoMock.Setup(r => r.GetChatByIdAsync(1)).ReturnsAsync(chat);
        _requestRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(request);

        var result = await _service.SendMessage(1, new MessageDTO { Context = "Hi" }, CreateUser(2, "Driver"));

        Assert.Equal(400, result.StatusCode);
        Assert.Contains("Não é possível enviar mensagens neste chat, pois o pedido foi cancelado", result.Body.ToString());
    }

    [Fact]
    public async Task CreateChatFromAcceptedBid_ShouldReturn400_WhenNoAcceptedBid()
    {
        _chatRepoMock.Setup(r => r.GetChatByTransportRequestIdAsync(1))
                     .ReturnsAsync((Chats)null);

        _chatRepoMock.Setup(r => r.GetAcceptedBidByRequestIdAsync(1))
                     .ReturnsAsync((Bid)null);

        var result = await _service.CreateChatFromAcceptedBid(1);

        Assert.Equal(400, result.StatusCode);
        Assert.Contains("Nenhuma bid aceite encontrada", result.Body.ToString());
    }

    [Fact]
    public async Task CreateChatFromAcceptedBid_ShouldReturn200_WhenChatCreated()
    {
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

        var result = await _service.CreateChatFromAcceptedBid(1);

        Assert.Equal(200, result.StatusCode);
        Assert.Contains("123", result.Body.ToString());
    }
}
