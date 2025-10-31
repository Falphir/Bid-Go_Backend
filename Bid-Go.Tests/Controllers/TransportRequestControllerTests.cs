using Bid_Go_Backend.Controllers;
using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.DTOs;

using Bid_Go_Backend.Data.Models.Enums;
using Bid_Go_Backend.Data.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Bid_Go.Tests.Controllers
{
    public class TransportRequestControllerTests
    {
        private readonly Mock<ITransportRequestRepository> _mockRepo;
        private readonly TransportRequestsController _controller;

        public TransportRequestControllerTests()
        {
            _mockRepo = new Mock<ITransportRequestRepository>();
            _controller = new TransportRequestsController(_mockRepo.Object);
        }

    

        [Fact]
        public async Task Create_ShouldReturnBadRequest_WhenModelInvalid()
        {
            _controller.ModelState.AddModelError("Origin", "Required");

            var dto = new CreateTransportRequestDTO();
            var result = await _controller.Create(dto);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Create_ShouldReturnBadRequest_WhenPickupAfterDelivery()
        {
            var dto = new CreateTransportRequestDTO
            {
                PickupDate = DateTime.Today.AddDays(2),
                DeliveryDate = DateTime.Today
            };

            var result = await _controller.Create(dto);
            var bad = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("A data de recolha deve ser anterior à data de entrega.",
                         bad.Value.GetType().GetProperty("message").GetValue(bad.Value));
        }

        [Fact]
        public async Task Create_ShouldReturnCreated_WhenValid()
        {
            var dto = new CreateTransportRequestDTO
            {
                Origin = "A",
                Destination = "B",
                Package = "Box",
                PickupDate = DateTime.Today,
                DeliveryDate = DateTime.Today.AddDays(1),
                Image = "img.png",
                Weight = 10,
                Volume = 5,
                MaxPrice = 30,
                CompanyId = 1
            };

            _mockRepo.Setup(r => r.CreateAsync(It.IsAny<TransportRequest>()))
                     .ReturnsAsync((TransportRequest req) =>
                     {
                         req.TransportRequestId = 1;
                         return req;
                     });

            var result = await _controller.Create(dto);

            var created = Assert.IsType<CreatedAtActionResult>(result);
            var returned = Assert.IsType<TransportRequest>(created.Value);
            Assert.Equal(1, returned.TransportRequestId);
        }

  

        [Fact]
        public async Task Update_ShouldReturnNotFound_WhenRequestDoesNotExist()
        {
            _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((TransportRequest)null);

            var dto = new UpdateTransportRequestDTO();
            var result = await _controller.Update(1, dto);

            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("Pedido de transporte não existe.",
                         notFound.Value.GetType().GetProperty("message").GetValue(notFound.Value));
        }

        [Fact]
        public async Task Update_ShouldReturnBadRequest_WhenStatusNotDraft()
        {
            var existing = new TransportRequest { TransportRequestId = 1, Status = ERequestStatus.Active };
            _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);

            var dto = new UpdateTransportRequestDTO();
            var result = await _controller.Update(1, dto);

            var bad = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Só é possível atualizar pedidos com estado DRAFT.",
                         bad.Value.GetType().GetProperty("message").GetValue(bad.Value));
        }

        [Fact]
        public async Task Update_ShouldReturnOk_WhenValid()
        {
            var existing = new TransportRequest
            {
                TransportRequestId = 1,
                Status = ERequestStatus.Draft,
                PickupDate = DateTime.Today,
                DeliveryDate = DateTime.Today.AddDays(1),
                MaxPrice = 50
            };
            _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);
            _mockRepo.Setup(r => r.UpdateAsync(1, It.IsAny<TransportRequest>()))
                     .ReturnsAsync((int id, TransportRequest req) => req);

            var dto = new UpdateTransportRequestDTO
            {
                MaxPrice = 60
            };

            var result = await _controller.Update(1, dto);

            var ok = Assert.IsType<OkObjectResult>(result);
            var returned = Assert.IsType<TransportRequest>(ok.Value);
            Assert.Equal(60, returned.MaxPrice);
        }



        [Fact]
        public async Task Delete_ShouldReturnNotFound_WhenRequestDoesNotExist()
        {
            _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((TransportRequest)null);
            var result = await _controller.Delete(1);

            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("Pedido não encontrado.",
                         notFound.Value.GetType().GetProperty("message").GetValue(notFound.Value));
        }

        [Fact]
        public async Task Delete_ShouldReturnConflict_WhenStatusNotActive()
        {
            var req = new TransportRequest { TransportRequestId = 1, Status = ERequestStatus.Draft };
            _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(req);

            var result = await _controller.Delete(1);

            var conflict = Assert.IsType<ConflictObjectResult>(result);
            Assert.Equal("Apenas pedidos ativos podem ser eliminados.",
                         conflict.Value.GetType().GetProperty("message").GetValue(conflict.Value));
        }

        [Fact]
        public async Task Delete_ShouldReturnOk_WhenActive()
        {
            var req = new TransportRequest { TransportRequestId = 1, Status = ERequestStatus.Active };
            _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(req);
            _mockRepo.Setup(r => r.DeleteAsync(1)).ReturnsAsync(true);

            var result = await _controller.Delete(1);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("Pedido eliminado com sucesso.",
                         ok.Value.GetType().GetProperty("message").GetValue(ok.Value));
        }

        [Fact]
        public async Task GetById_ShouldReturnNotFound_WhenRequestDoesNotExist()
        {
            _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((TransportRequest)null);

            var result = await _controller.GetById(1);

            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("Pedido de transporte não existe", notFound.Value);
        }

        [Fact]
        public async Task GetById_ShouldReturnOk_WhenExists()
        {
            var req = new TransportRequest
            {
                TransportRequestId = 1,
                Origin = "A",
                Destination = "B",
                Package = "Box",
                PickupDate = DateTime.Today,
                DeliveryDate = DateTime.Today.AddDays(1),
                Weight = 10,
                Volume = 5,
                Image = "img.png",
                MaxPrice = 50
            };
            _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(req);

            var result = await _controller.GetById(1);

            var ok = Assert.IsType<OkObjectResult>(result);
            var dto = Assert.IsType<TransportRequestResponseDTO>(ok.Value);
            Assert.Equal("A", dto.Origin);
            Assert.Equal("B", dto.Destination);
        }

 


        [Fact]
        public async Task GetByCompany_ShouldReturnNotFound_WhenNoRequests()
        {
            _mockRepo.Setup(r => r.GetAllByCompanyAsync(1)).ReturnsAsync((List<TransportRequest>)null);

            var result = await _controller.GetByCompany(1);

            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("Não foram encontrados pedidos de transporte da company", notFound.Value);
        }

        [Fact]
        public async Task GetByCompany_ShouldReturnOk_WhenExists()
        {
            var list = new List<TransportRequest>
            {
                new TransportRequest
                {
                    TransportRequestId = 1,
                    Origin = "A",
                    Destination = "B",
                    Package = "Box",
                    PickupDate = DateTime.Today,
                    DeliveryDate = DateTime.Today.AddDays(1),
                    Weight = 10,
                    Volume = 5,
                    Image = "img.png",
                    MaxPrice = 50,
                    Status = ERequestStatus.Draft
                }
            };

            _mockRepo.Setup(r => r.GetAllByCompanyAsync(1)).ReturnsAsync(list);

            var result = await _controller.GetByCompany(1);

            var ok = Assert.IsType<OkObjectResult>(result);
            var returned = Assert.IsType<List<TransportRequestResponseDTO>>(ok.Value);
            Assert.Single(returned);
        }



        [Fact]
        public async Task Update_ShouldReturnBadRequest_WhenPickupDateAfterDeliveryDate()
        {
            var dto = new UpdateTransportRequestDTO
            {
                PickupDate = DateTime.Today.AddDays(2),
                DeliveryDate = DateTime.Today
            };
            _mockRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
                     .ReturnsAsync(new TransportRequest { Status = ERequestStatus.Draft });

            var result = await _controller.Update(1, dto);

            var bad = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("A data de recolha deve ser anterior à data de entrega.", bad.Value.GetType().GetProperty("message").GetValue(bad.Value));
        }

        [Fact]
        public async Task Update_ShouldReturnBadRequest_WhenImageIsEmpty()
        {
            var dto = new UpdateTransportRequestDTO { Image = "" };
            _mockRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
                     .ReturnsAsync(new TransportRequest { Status = ERequestStatus.Draft });

            var result = await _controller.Update(1, dto);

            var bad = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("A imagem é obrigatória para publicar o pedido.", bad.Value.GetType().GetProperty("message").GetValue(bad.Value));
        }

        [Fact]
        public async Task Update_ShouldReturnBadRequest_WhenWeightInvalid()
        {
            var dto = new UpdateTransportRequestDTO { Weight = 0 };
            _mockRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
                     .ReturnsAsync(new TransportRequest { Status = ERequestStatus.Draft });

            var result = await _controller.Update(1, dto);

            var bad = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("O peso deve ser superior a zero.", bad.Value.GetType().GetProperty("message").GetValue(bad.Value));
        }

        [Fact]
        public async Task Update_ShouldReturnBadRequest_WhenVolumeInvalid()
        {
            var dto = new UpdateTransportRequestDTO { Volume = 0 };
            _mockRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
                     .ReturnsAsync(new TransportRequest { Status = ERequestStatus.Draft });

            var result = await _controller.Update(1, dto);

            var bad = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("O volume deve ser superior a zero.", bad.Value.GetType().GetProperty("message").GetValue(bad.Value));
        }

        [Fact]
        public async Task Update_ShouldReturnBadRequest_WhenMaxPriceInvalid()
        {
            var dto = new UpdateTransportRequestDTO { MaxPrice = 10 };
            _mockRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
                     .ReturnsAsync(new TransportRequest { Status = ERequestStatus.Draft });

            var result = await _controller.Update(1, dto);

            var bad = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("O preço deve ser superior ou igual a vinte.", bad.Value.GetType().GetProperty("message").GetValue(bad.Value));
        }

        [Fact]
        public async Task Update_ShouldUpdateFields_WhenAllValid()
        {
            var dto = new UpdateTransportRequestDTO
            {
                Origin = "Origem",
                Destination = "Destino",
                Package = "Pacote",
                PickupDate = DateTime.Today,
                DeliveryDate = DateTime.Today.AddDays(1),
                Weight = 10,
                Volume = 5,
                Length = 2,
                Width = 2,
                Height = 2,
                Image = "img.png",
                MaxPrice = 50
            };

            var existing = new TransportRequest { Status = ERequestStatus.Draft };
            _mockRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(existing);
            _mockRepo.Setup(r => r.UpdateAsync(It.IsAny<int>(), It.IsAny<TransportRequest>()))
                     .ReturnsAsync((int id, TransportRequest tr) => tr);

            var result = await _controller.Update(1, dto);

            var ok = Assert.IsType<OkObjectResult>(result);
            var updated = Assert.IsType<TransportRequest>(ok.Value);

            Assert.Equal("Origem", updated.Origin);
            Assert.Equal("Destino", updated.Destination);
            Assert.Equal("Pacote", updated.Package);
            Assert.Equal(dto.PickupDate, updated.PickupDate);
            Assert.Equal(dto.DeliveryDate, updated.DeliveryDate);
            Assert.Equal(dto.Weight, updated.Weight);
            Assert.Equal(dto.Volume, updated.Volume);
            Assert.Equal(dto.Length, updated.Length);
            Assert.Equal(dto.Width, updated.Width);
            Assert.Equal(dto.Height, updated.Height);
            Assert.Equal(dto.Image, updated.Image);
            Assert.Equal(dto.MaxPrice, updated.MaxPrice);
        }
    }
}

