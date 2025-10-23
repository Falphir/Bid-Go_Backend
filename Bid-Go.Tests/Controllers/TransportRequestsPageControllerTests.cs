using Bid_Go_Backend.Controllers;
using Bid_Go_Backend.Data;
using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.DTOs;
using Bid_Go_Backend.Data.Models.Enums;
using Bid_Go_Backend.Data.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;



namespace Bid_Go_Backend.Tests.Controllers;

public class TransportRequestsPageControllerTests
{
   
        private readonly Mock<ITransportRequestsPageRepository> _mockRepo;
    private readonly TransportRequestsPageController _controller;

    public TransportRequestsPageControllerTests()
    {
        _mockRepo = new Mock<ITransportRequestsPageRepository>();
        _controller = new TransportRequestsPageController(_mockRepo.Object);
    }


    // ✅ GET /api/PageRequests
    [Fact]
    public async Task GetActive_ShouldReturnOk_WhenRequestsExist()
    {
        var requests = new List<TransportRequest>
            {
                new TransportRequest
                {
                    Origin = "Lisboa",
                    Destination = "Porto",
                    Package = "Caixa",
                    PickupDate = DateTime.Now,
                    DeliveryDate = DateTime.Now.AddDays(2),
                    MaxPrice = 150,
                    Image = "imagem.png"
                }
            };

        _mockRepo.Setup(r => r.GetActiveAsync(null, null, null, null))
                 .ReturnsAsync(requests);

        var result = await _controller.GetActive(null, null, null, null);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dtoList = Assert.IsAssignableFrom<IEnumerable<TransportRequestsPageDTO>>(ok.Value);

        Assert.Single(dtoList);
        Assert.Equal("Lisboa", dtoList.First().Origin);
        Assert.Equal("Porto", dtoList.First().Destination);
    }

    [Fact]
    public async Task GetActive_ShouldReturnMessage_WhenNoRequestsExist()
    {
        _mockRepo.Setup(r => r.GetActiveAsync(null, null, null, null))
                 .ReturnsAsync(new List<TransportRequest>());

        var result = await _controller.GetActive(null, null, null, null);

        var ok = Assert.IsType<OkObjectResult>(result.Result);

        // Serializa + desserializa para um objeto dinâmico
        var json = System.Text.Json.JsonSerializer.Serialize(ok.Value);
        var doc = System.Text.Json.JsonDocument.Parse(json);
        var message = doc.RootElement.GetProperty("message").GetString();

        Assert.Equal("Não existem pedidos ativos no momento.", message);

    }


    // ✅ GET /api/PageRequests/{id}
    [Fact]
    public async Task GetById_ShouldReturnOk_WhenRequestExists()
    {
        var request = new TransportRequest
        {
            Origin = "Lisboa",
            Destination = "Porto",
            Package = "Encomenda",
            PickupDate = DateTime.Today,
            DeliveryDate = DateTime.Today.AddDays(1),
            Weight = 10,
            Volume = 2,
            Length = 100,
            Width = 50,
            Height = 40,
            MaxPrice = 200,
            Image = "imagem.jpg"
        };

        _mockRepo.Setup(r => r.GetByIdAsync(1))
                 .ReturnsAsync(request);

        var result = await _controller.GetById(1);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<TransportRequestResponseDTO>(ok.Value);

        Assert.Equal("Lisboa", dto.Origin);
        Assert.Equal("Porto", dto.Destination);
        Assert.Equal(10, dto.Weight);
    }

    [Fact]
    public async Task GetById_ShouldReturnNotFound_WhenRequestDoesNotExist()
    {
        _mockRepo.Setup(r => r.GetByIdAsync(1))
                 .ReturnsAsync((TransportRequest)null);

        var result = await _controller.GetById(1);

        var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal("Pedido de transporte não existe", notFound.Value);
    }

    [Fact]
    public async Task GetById_ShouldReturnServerError_WhenExceptionOccurs()
    {
        _mockRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
                 .ThrowsAsync(new Exception("Erro de BD"));

        var result = await _controller.GetById(1);
        var error = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, error.StatusCode);

        var json = System.Text.Json.JsonSerializer.Serialize(error.Value);
        var doc = System.Text.Json.JsonDocument.Parse(json);
        var message = doc.RootElement.GetProperty("message").GetString();

        Assert.Contains("Erro inesperado", message);
    }

    }
