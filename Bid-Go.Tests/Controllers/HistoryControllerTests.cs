using Bid_Go_Backend.Controllers;
using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.DTOs;
using Bid_Go_Backend.Data.Models.Enums;
using Bid_Go_Backend.Data.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Bid_Go_Backend.Tests.Controllers
{
    public class HistoryControllerTests
    {
        private readonly Mock<IHistoryRepository> _mockRepo;
        private readonly Mock<ILogger<HistoryController>> _mockLogger;
        private readonly HistoryController _controller;

        public HistoryControllerTests()
        {
            _mockRepo = new Mock<IHistoryRepository>();
            _mockLogger = new Mock<ILogger<HistoryController>>();
            _controller = new HistoryController(_mockRepo.Object, _mockLogger.Object);
        }


        // - Teste: o método deve retornar OkObjectResult com a lista de BidHistoryDTO.
        // Como:
        // - Setup: o repositório devolve uma lista com um elemento.
        // - Act: chama GetDriverHistory(driverId).
        // - Assert: verifica OkObjectResult e que a lista retornada contém o elemento esperado.
        [Fact]
        public async Task GetDriverHistory_ShouldReturnOkWithList_WhenHistoryExists()
        {
            var list = new List<BidHistoryDTO>
            {
                new BidHistoryDTO
                {
                    CompanyName = "Co",
                    Package = "Box",
                    Date = DateTime.UtcNow,
                    Destination = "Porto",
                    Value = 12.5m,
                    Status = EBidStatus.Accepted,
                    Rating = 4.0m
                }
            };

            _mockRepo.Setup(r => r.GetDriverHistoryAsync(1)).ReturnsAsync(list);

            var result = await _controller.GetDriverHistory(1);

            var ok = Assert.IsType<OkObjectResult>(result);
            var returned = Assert.IsType<List<BidHistoryDTO>>(ok.Value);
            Assert.Single(returned);
            Assert.Equal("Co", returned[0].CompanyName);
        }


        // - Teste: o método deve retornar NotFoundObjectResult com a mensagem esperada.
        // Como:
        // - Setup: o repositório devolve null.
        // - Act: chama GetDriverHistory(driverId).
        // - Assert: verifica NotFoundObjectResult e propriedade "message" com o texto correto.
        [Fact]
        public async Task GetDriverHistory_ShouldReturnNotFound_WhenHistoryIsNullOrEmpty()
        {
            _mockRepo.Setup(r => r.GetDriverHistoryAsync(2)).ReturnsAsync((List<BidHistoryDTO>?)null);

            var result = await _controller.GetDriverHistory(2);

            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            var val = notFound.Value!;
            var prop = val.GetType().GetProperty("message");
            Assert.NotNull(prop);
            Assert.Equal("Nenhum histórico encontrado para este motorista.", prop.GetValue(val));
        }


        // - Teste: o método deve retornar ObjectResult com StatusCode 500 e mensagem genérica.
        // Como:
        // - Setup: o repositório lança Exception.
        // - Act: chama GetDriverHistory(driverId).
        // - Assert: verifica ObjectResult com StatusCode 500 e propriedade "message" com o texto genérico.
        [Fact]
        public async Task GetDriverHistory_ShouldReturnStatus500_OnException()
        {
            _mockRepo.Setup(r => r.GetDriverHistoryAsync(3)).ThrowsAsync(new Exception("fail"));

            var result = await _controller.GetDriverHistory(3);

            var obj = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, obj.StatusCode);
            var val = obj.Value!;
            var prop = val.GetType().GetProperty("message");
            Assert.NotNull(prop);
            Assert.Equal("Ocorreu um erro ao obter o histórico do motorista.", prop.GetValue(val));
        }

        // - Teste: o método deve retornar OkObjectResult com a lista de TransportHistoryDTO.
        // Como:
        // - Setup: o repositório devolve uma lista com um elemento.
        // - Act: chama GetCompanyHistory(companyId).
        // - Assert: verifica OkObjectResult e que a lista retornada contém o elemento esperado.
        [Fact]
        public async Task GetCompanyHistory_ShouldReturnOkWithList_WhenHistoryExists()
        {
            var list = new List<TransportHistoryDTO>
            {
                new TransportHistoryDTO
                {
                    TransportRequestId = 1,
                    Package = "Box",
                    Name = "Cliente",
                    Date = DateTime.UtcNow,
                    Destination = "Lisboa",
                    Price = 20m,
                    Status = "Concluído"
                }
            };

            _mockRepo.Setup(r => r.GetTransportHistoryAsync(10)).ReturnsAsync(list);

            var result = await _controller.GetCompanyHistory(10);

            var ok = Assert.IsType<OkObjectResult>(result);
            var returned = Assert.IsType<List<TransportHistoryDTO>>(ok.Value);
            Assert.Single(returned);
            Assert.Equal(1, returned[0].TransportRequestId);
        }

        // - Teste: o método deve retornar NotFoundObjectResult com a mensagem esperada.
        // Como:
        // - Setup: o repositório devolve uma lista vazia.
        // - Act: chama GetCompanyHistory(companyId).
        // - Assert: verifica NotFoundObjectResult e propriedade "message" com o texto correto.
        [Fact]
        public async Task GetCompanyHistory_ShouldReturnNotFound_WhenHistoryIsNullOrEmpty()
        {
            _mockRepo.Setup(r => r.GetTransportHistoryAsync(11)).ReturnsAsync(new List<TransportHistoryDTO>());

            var result = await _controller.GetCompanyHistory(11);

            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            var val = notFound.Value!;
            var prop = val.GetType().GetProperty("message");
            Assert.NotNull(prop);
            Assert.Equal("Nenhum histórico encontrado para esta empresa.", prop.GetValue(val));
        }


        // - Teste: o método deve retornar ObjectResult com StatusCode 500 e mensagem genérica.
        // Como:
        // - Setup: o repositório lança Exception.
        // - Act: chama GetCompanyHistory(companyId).
        // - Assert: verifica ObjectResult com StatusCode 500 e propriedade "message" com o texto genérico.
        [Fact]
        public async Task GetCompanyHistory_ShouldReturnStatus500_OnException()
        {
            _mockRepo.Setup(r => r.GetTransportHistoryAsync(12)).ThrowsAsync(new Exception("fail"));

            var result = await _controller.GetCompanyHistory(12);

            var obj = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, obj.StatusCode);
            var val = obj.Value!;
            var prop = val.GetType().GetProperty("message");
            Assert.NotNull(prop);
            Assert.Equal("Ocorreu um erro ao obter o histórico da empresa.", prop.GetValue(val));
        }
    }
}