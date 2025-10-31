using Bid_Go_Backend.Controllers;

using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.DTOs;
using Bid_Go_Backend.Data.Models.Enums;
using Bid_Go_Backend.Data.Repositories.Interfaces;
using Bid_Go_Backend.Repositories.Interface;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Bid_Go_Tests.Controllers
{
    public class ChatControllerTests
    {
        private readonly Mock<IChatRepository> _mockRepo;
        private readonly ChatController _controller;

        public ChatControllerTests()
        {
            _mockRepo = new Mock<IChatRepository>();
            _controller = new ChatController(_mockRepo.Object);
        }


        [Fact]
        public async Task GetChat_ShouldReturnNotFound_WhenChatDoesNotExist()
        {
            // Arrange
            _mockRepo.Setup(r => r.GetChatByRequestIdAsync(1))
                     .ReturnsAsync((Chats)null);

            // Act
            var result = await _controller.GetChat(1);

            // Assert
            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            var messageProperty = notFound.Value.GetType().GetProperty("message");
            var messageValue = messageProperty?.GetValue(notFound.Value, null) as string;
            Assert.Equal("Chat não encontrado.", messageValue);
        }

        [Fact]
        public async Task GetChat_ShouldReturnOk_WithChatDTO()
        {
            // Arrange
            var chat = new Chats
            {
                ChatId = 1,
                Status = EChatStatus.Active,
                TransportRequestId = 5,
                Messages = new List<Message>
                {
                    new Message { Context = "Bom Dia Sr Pedro", DriverId = 1, CompanyId = 2 },
                    new Message { Context = "Olá, Bom dia", DriverId = 0, CompanyId = 2 }
                }
            };
            _mockRepo.Setup(r => r.GetChatByRequestIdAsync(5)).ReturnsAsync(chat);

            // Act
            var result = await _controller.GetChat(5);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            var dto = Assert.IsType<ChatDTO>(ok.Value);
            Assert.Equal(chat.ChatId, dto.ChatId);
            Assert.Equal(2, dto.Messages.Count);
            Assert.Equal("Bom Dia Sr Pedro", dto.Messages.First().Context);
        }

        [Fact]
        public async Task GetChat_ShouldReturnStatus500_OnException()
        {
            // Arrange
            _mockRepo.Setup(r => r.GetChatByRequestIdAsync(It.IsAny<int>()))
                     .ThrowsAsync(new Exception());

            // Act
            var result = await _controller.GetChat(1);
            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, objectResult.StatusCode);

            var messageProperty = objectResult.Value.GetType().GetProperty("message");
            var messageValue = messageProperty?.GetValue(objectResult.Value, null) as string;
            Assert.Equal("Ocorreu um erro inesperado.", messageValue);
        }


        [Fact]
        public async Task SendMessage_ShouldReturnOk_WithMessageDTO()
        {
            // Arrange
            var dto = new MessageDTO { Context = "Boa Tarde", DriverId = 1 };
            var resultMessage = new Message
            {
                Context = "Boa Tarde",
                DriverId = 1,
                CompanyId = 0,
                TimeStamp = DateTime.UtcNow
            };

            _mockRepo.Setup(r => r.SendMessageAsync(It.IsAny<Message>()))
                     .ReturnsAsync(resultMessage);

            // Act
            var result = await _controller.SendMessage(1, dto);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            var returnedDto = Assert.IsType<MessageDTO>(ok.Value);
            Assert.Equal("Boa Tarde", returnedDto.Context);
            Assert.Equal(1, returnedDto.DriverId);
        }

        [Fact]
        public async Task SendMessage_ShouldReturnBadRequest_WhenInvalidOperationException()
        {
            var dto = new MessageDTO { Context = "Boa Tarde" };
            _mockRepo.Setup(r => r.SendMessageAsync(It.IsAny<Message>()))
                     .ThrowsAsync(new InvalidOperationException("Invalid"));

            var result = await _controller.SendMessage(1, dto);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var messageProperty = badRequest.Value.GetType().GetProperty("message");
            var messageValue = messageProperty?.GetValue(badRequest.Value, null) as string;
            Assert.Equal("Invalid", messageValue);
        }

        [Fact]
        public async Task SendMessage_ShouldReturnNotFound_WhenKeyNotFoundException()
        {
            // Arrange
            var dto = new MessageDTO { Context = "Boa Tarde" };
            _mockRepo.Setup(r => r.SendMessageAsync(It.IsAny<Message>()))
                     .ThrowsAsync(new KeyNotFoundException("Not found"));

            // Act
            var result = await _controller.SendMessage(1, dto);

            // Assert
            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            var messageProperty = notFound.Value.GetType().GetProperty("message");
            var messageValue = messageProperty?.GetValue(notFound.Value, null) as string;
            Assert.Equal("Not found", messageValue);
        }

        [Fact]
        public async Task SendMessage_ShouldReturnStatus500_OnException()
        {
            // Arrange
            var dto = new MessageDTO { Context = "Boa Tarde" };
            _mockRepo.Setup(r => r.SendMessageAsync(It.IsAny<Message>()))
                     .ThrowsAsync(new Exception());

            // Act
            var result = await _controller.SendMessage(1, dto);
            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, objectResult.StatusCode);

            var messageProperty = objectResult.Value.GetType().GetProperty("message");
            var messageValue = messageProperty?.GetValue(objectResult.Value, null) as string;
            Assert.Equal("Ocorreu um erro inesperado.", messageValue);
        }


        [Fact]
        public async Task GetMessages_ShouldReturnOk_WithListOfMessages()
        {
            // Arrange
            var messages = new List<Message>
            {
                new Message { Context = "Boa Tarde", DriverId = 1 },
                new Message { Context = "Boa Tarde, Tudo bem ?", CompanyId = 2 }
            };
            _mockRepo.Setup(r => r.GetMessagesAsync(1)).ReturnsAsync(messages);

            // Act
            var result = await _controller.GetMessages(1);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            var returnedMessages = Assert.IsAssignableFrom<IEnumerable<Message>>(ok.Value);
            Assert.Equal(2, returnedMessages.Count());
        }


        [Fact]
        public async Task CreateChatFromAcceptedBid_ShouldReturnOk_WithChat()
        {
            // Arrange

            var chat = new Chats { ChatId = 1, Status = EChatStatus.Active };
            _mockRepo.Setup(r => r.CreateChatFromAcceptedBidAsync(5))
                     .ReturnsAsync(chat);

            // Act
            var result = await _controller.CreateChatFromAcceptedBid(5);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            var returnedChat = Assert.IsType<Chats>(ok.Value);
            Assert.Equal(1, returnedChat.ChatId);
        }
    }
}
