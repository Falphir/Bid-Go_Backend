using Bid_Go_Backend.Controllers;
using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.DTOs;
using Bid_Go_Backend.Data.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Xunit;

namespace Bid_Go_Backend.Tests.Controllers
{
    public class AutomaticSelectionAlgorithmControllerTests
    {
        private readonly Mock<IAutomaticSelectionAlgorithmRepository> _mockRepo;
        private readonly AutomaticSelectionAlgorithmController _controller;

        public AutomaticSelectionAlgorithmControllerTests()
        {
            _mockRepo = new Mock<IAutomaticSelectionAlgorithmRepository>();
            _controller = new AutomaticSelectionAlgorithmController(_mockRepo.Object);
        }

        // Teste:
        // - Cenário: o algoritmo não teve sucesso (Success = false).
        // - Objetivo: o endpoint deve retornar BadRequest com a mensagem do resultado.
        // - Como: mock devolve AutomaticSelectionResult com Success = false e Message preenchida.
        [Fact]
        public async Task ExecuteAlgorithm_ShouldReturnBadRequest_WhenSelectionFails()
        {
            var result = new AutomaticSelectionResult
            {
                SelectedBid = null,
                Message = "No eligible bids"
            };

            _mockRepo.Setup(r => r.ExecuteAutomaticSelectionAsync(42)).ReturnsAsync(result);

            var actionResult = await _controller.ExecuteAlgorithm(42);

            var bad = Assert.IsType<BadRequestObjectResult>(actionResult);
            Assert.Equal("No eligible bids", bad.Value);
        }

        // Teste:
        // - Cenário: o algoritmo seleciona uma licitação com sucesso.
        // - Objetivo: o endpoint deve retornar Ok com o objeto que contém:
        // - Como: mock devolve AutomaticSelectionResult com Success = true e SelectedBid populado.
        [Fact]
        public async Task ExecuteAlgorithm_ShouldReturnOk_WithSelectedBid_WhenSuccess()
        {
            var driver = new Driver
            {
                Name = "DriverName",
                Email = "d@example.com",
                PhoneNumber = 912345678
            };

            var bid = new Bid
            {
                BidId = 1,
                Value = 99.50m,
                DriverId = 5,
                Driver = driver
            };

            var result = new AutomaticSelectionResult
            {
                SelectedBid = bid,
                Message = "Selected"
           
            };

            _mockRepo.Setup(r => r.ExecuteAutomaticSelectionAsync(100)).ReturnsAsync(result);

            var actionResult = await _controller.ExecuteAlgorithm(100);

            var ok = Assert.IsType<OkObjectResult>(actionResult);
            var val = ok.Value!;
            var msgProp = val.GetType().GetProperty("message", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            Assert.NotNull(msgProp);
            Assert.Equal("Automatic selection executed successfully.", msgProp.GetValue(val));

            var selectedProp = val.GetType().GetProperty("selectedBid", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            Assert.NotNull(selectedProp);

            var bidDto = selectedProp.GetValue(val);
            Assert.NotNull(bidDto);

            var bidDtoType = bidDto!.GetType();
            var bidIdProp = bidDtoType.GetProperty("bidId", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            var valueProp = bidDtoType.GetProperty("value", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            var driverProp = bidDtoType.GetProperty("driver", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

            Assert.NotNull(bidIdProp);
            Assert.NotNull(valueProp);
            Assert.NotNull(driverProp);

            Assert.Equal(1, (int)bidIdProp.GetValue(bidDto)!);
            Assert.Equal(99.50m, (decimal)valueProp.GetValue(bidDto)!);

            var driverDto = driverProp.GetValue(bidDto);
            Assert.NotNull(driverDto);

            var driverDtoType = driverDto!.GetType();
            var idProp = driverDtoType.GetProperty("id", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            var nameProp = driverDtoType.GetProperty("name", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            var emailProp = driverDtoType.GetProperty("email", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            var phoneProp = driverDtoType.GetProperty("phoneNumber", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

            Assert.NotNull(idProp);
            Assert.NotNull(nameProp);
            Assert.NotNull(emailProp);
            Assert.NotNull(phoneProp);

            Assert.Equal(5, (int)idProp.GetValue(driverDto)!);
            Assert.Equal("DriverName", (string)nameProp.GetValue(driverDto)!);
            Assert.Equal("d@example.com", (string)emailProp.GetValue(driverDto)!);
            Assert.Equal(912345678, Convert.ToInt32(phoneProp.GetValue(driverDto)!));
        }

        // Teste:
        // - Cenário: o repositório devolve um resultado nulo (defensivo).
        // - Objetivo: garantir que um NullReferenceException não é mascarado pelo teste;
        // - Como: mock devolve null -> espera-se que a chamada lance (ou tratar conforme regra de negócio).
        [Fact]
        public async Task ExecuteAlgorithm_ShouldThrow_WhenRepositoryReturnsNull()
        {
            _mockRepo.Setup(r => r.ExecuteAutomaticSelectionAsync(7)).ReturnsAsync((AutomaticSelectionResult?)null);

            await Assert.ThrowsAsync<NullReferenceException>(async () => await _controller.ExecuteAlgorithm(7));
        }
    }
}