using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.DTOs;
using Bid_Go_Backend.Data.Models.Enums;
using Bid_Go_Backend.Services.Transport_Request;
using Moq;
using Stripe;
using Xunit;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Text;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using Bid_Go_Backend.Services.Interfaces;
using Bid_Go_Backend.Services.Transport_Request;
using Bid_Go_Backend.Repositories.Interfaces;

namespace Bid_Go.Tests.Unit.Services
{
 /// <summary>
 /// Unit tests for TransportRequestService covering creation, update, deletion and retrieval validations.
 /// </summary>
 public class TransportRequestServiceTests
 {
  private readonly Mock<ITransportRequestRepository> _repositoryMock;
  private readonly Mock<ICloudflareR2Service> _mockR2;
  private readonly TransportRequestService _service;

  public TransportRequestServiceTests()
  {
   _repositoryMock = new Mock<ITransportRequestRepository>();

   _mockR2 = new Mock<ICloudflareR2Service>();
   _mockR2.Setup(r => r.UploadImageAsync(It.IsAny<IFormFile>()))
          .ReturnsAsync((IFormFile f) => "https://cdn.example.com/" + f.FileName);
   _mockR2.Setup(r => r.DeleteImageAsync(It.IsAny<string>()))
          .Returns(Task.CompletedTask);

   _service = new TransportRequestService(_repositoryMock.Object, _mockR2.Object);

  }

  private static IFormFile MakeFormFile(string fileName = "image.jpg")
  {
   var content = "fake image content";
   var bytes = Encoding.UTF8.GetBytes(content);
   var stream = new MemoryStream(bytes);
   stream.Position = 0;
   return new FormFile(stream, 0, stream.Length, "file", fileName)
   {
    Headers = new HeaderDictionary(),
    ContentType = "image/jpeg"
   };
  }

  [Fact]
  public async Task CreateAsync_ShouldThrow_WhenPickupAfterDelivery()
  {
   // Arrange
   var dto = new CreateTransportRequestDTO { PickupDate = DateTime.UtcNow.AddDays(2), DeliveryDate = DateTime.UtcNow.AddDays(1), BiddingStartDate = DateTime.UtcNow, BiddingEndDate = DateTime.UtcNow.AddHours(2), Weight =10, Volume =10, MaxPrice =50, Origin = "O", Destination = "D", Package = "P", Length =10, Width =10, Height =10, CompanyId =1, IsAutomaticSelectionEnabled = false };
   var file = MakeFormFile();

   // Act + Assert
   await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateAsync(dto, file));
  }

  [Fact]
  public async Task CreateAsync_ShouldThrow_WhenBiddingStartAfterEnd()
  {
   var dto = new CreateTransportRequestDTO
   {
    PickupDate = DateTime.UtcNow.AddDays(3),
    DeliveryDate = DateTime.UtcNow.AddDays(5),
    BiddingStartDate = DateTime.UtcNow.AddHours(5),
    BiddingEndDate = DateTime.UtcNow.AddHours(1), // inválido
    Weight = 10,
    Volume = 10,
    MaxPrice = 50,
    Origin = "O",
    Destination = "D",
    Package = "P",
    Length = 10,
    Width = 10,
    Height = 10,
    CompanyId = 1,
    IsAutomaticSelectionEnabled = false
   };

   var file = MakeFormFile();

   var ex = await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateAsync(dto, file));
   Assert.Equal("A data de início das licitações deve ser anterior à data de fim.", ex.Message);
  }

  [Fact]
  public async Task CreateAsync_ShouldThrow_WhenPickupBeforeBiddingEnd()
  {
   var dto = new CreateTransportRequestDTO
   {
    PickupDate = DateTime.UtcNow.AddDays(1),
    DeliveryDate = DateTime.UtcNow.AddDays(3),
    BiddingStartDate = DateTime.UtcNow,
    BiddingEndDate = DateTime.UtcNow.AddDays(2),
    Weight = 10,
    Volume = 10,
    MaxPrice = 50,
    Origin = "O",
    Destination = "D",
    Package = "P",
    Length = 10,
    Width = 10,
    Height = 10,
    CompanyId = 1,
    IsAutomaticSelectionEnabled = false
   };

   var file = MakeFormFile();

   var ex = await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateAsync(dto, file));
   Assert.Equal("A data de recolha deve ser posterior ao fim das licitações.", ex.Message);
  }

  [Fact]
  public async Task CreateAsync_ShouldThrow_WhenImageIsMissing()
  {
   var dto = new CreateTransportRequestDTO
   {
    PickupDate = DateTime.UtcNow.AddDays(2),
    DeliveryDate = DateTime.UtcNow.AddDays(3),
    BiddingStartDate = DateTime.UtcNow,
    BiddingEndDate = DateTime.UtcNow.AddHours(1),
    Weight = 10,
    Volume = 10,
    MaxPrice = 50,
    Origin = "O",
    Destination = "D",
    Package = "P",
    Length = 10,
    Width = 10,
    Height = 10,
    CompanyId = 1,
    IsAutomaticSelectionEnabled = false
   };

   IFormFile? file = null; // missing

   var exception = await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateAsync(dto, file!));
   Assert.Equal("A imagem é obrigatória.", exception.Message);
  }

  [Fact]
  public async Task CreateAsync_ShouldReturnCreatedRequest_WhenValid()
  {
   var dto = new CreateTransportRequestDTO
   {
    Origin = "Lisboa",
    Destination = "Porto",
    Package = "Caixa",
    PickupDate = DateTime.UtcNow.AddDays(3),
    DeliveryDate = DateTime.UtcNow.AddDays(5),
    BiddingStartDate = DateTime.UtcNow,
    BiddingEndDate = DateTime.UtcNow.AddDays(2),
    Weight = 10,
    Volume = 10,
    Length = 50,
    Width = 30,
    Height = 40,
    MaxPrice = 60,
    CompanyId = 1,
    IsAutomaticSelectionEnabled = false
   };

   var expected = new TransportRequest { TransportRequestId = 1, Origin = dto.Origin };

   _repositoryMock
       .Setup(r => r.CreateAsync(It.IsAny<TransportRequest>()))
       .ReturnsAsync(expected);

   var file = MakeFormFile();

   var result = await _service.CreateAsync(dto, file);

   Assert.NotNull(result);
   Assert.Equal(expected.TransportRequestId, result.TransportRequestId);
   Assert.Equal("Lisboa", result.Origin);
   _repositoryMock.Verify(r => r.CreateAsync(It.IsAny<TransportRequest>()), Times.Once);
  }

  [Fact]
  public async Task CreateAsync_ShouldThrow_WhenDimensionsAreInvalid()
  {
   var dto = new CreateTransportRequestDTO
   {
    PickupDate = DateTime.UtcNow.AddDays(3),
    DeliveryDate = DateTime.UtcNow.AddDays(5),
    BiddingStartDate = DateTime.UtcNow,
    BiddingEndDate = DateTime.UtcNow.AddDays(2),
    Weight = 10,
    Volume = 10,
    Length = 0, // inválido
    Width = 30,
    Height = 40,
    MaxPrice = 50,
    Origin = "O",
    Destination = "D",
    Package = "P",
    CompanyId = 1,
    IsAutomaticSelectionEnabled = false
   };

   var file = MakeFormFile();

   var ex = await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateAsync(dto, file));
   Assert.Equal("As dimensões devem ser superiores a zero.", ex.Message);
  }

  [Fact]
  public async Task CreateAsync_ShouldThrow_WhenVolumeIsInvalid()
  {
   var dto = new CreateTransportRequestDTO
   {
    PickupDate = DateTime.UtcNow.AddDays(3),
    DeliveryDate = DateTime.UtcNow.AddDays(5),
    BiddingStartDate = DateTime.UtcNow,
    BiddingEndDate = DateTime.UtcNow.AddDays(2),
    Weight = 10,
    Volume = 0, // inválido
    Length = 50,
    Width = 30,
    Height = 40,
    MaxPrice = 60,
    Origin = "O",
    Destination = "D",
    Package = "P",
    CompanyId = 1,
    IsAutomaticSelectionEnabled = false
   };

   var file = MakeFormFile();

   var ex = await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateAsync(dto, file));
   Assert.Equal("Peso e volume devem ser superiores a zero.", ex.Message);
  }




  [Fact]
  public async Task UpdateAsync_ShouldThrow_WhenRequestNotFound()
  {
   _repositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
       .ReturnsAsync((TransportRequest?)null);

   await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.UpdateAsync(1, new UpdateTransportRequestDTO(), null));
  }

  [Fact]
  public async Task UpdateAsync_ShouldThrow_WhenStatusNotDraft()
  {
   var existing = new TransportRequest { Status = ERequestStatus.Active };
   _repositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);

   await Assert.ThrowsAsync<InvalidOperationException>(() => _service.UpdateAsync(1, new UpdateTransportRequestDTO(), null));
  }

  [Fact]
  public async Task UpdateAsync_ShouldThrow_WhenPickupAfterDelivery()
  {
   var existing = new TransportRequest { Status = ERequestStatus.Draft };
   _repositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);

   var dto = new UpdateTransportRequestDTO
   {
    PickupDate = DateTime.UtcNow.AddDays(5),
    DeliveryDate = DateTime.UtcNow.AddDays(1)
   };

   var ex = await Assert.ThrowsAsync<ArgumentException>(() => _service.UpdateAsync(1, dto, null));
   Assert.Equal("A data de recolha deve ser anterior à data de entrega.", ex.Message);
  }

  [Fact]
  public async Task UpdateAsync_ShouldThrow_WhenBiddingStartAfterEnd()
  {
   var existing = new TransportRequest { Status = ERequestStatus.Draft };
   _repositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);

   var dto = new UpdateTransportRequestDTO
   {
    BiddingStartDate = DateTime.UtcNow.AddDays(2),
    BiddingEndDate = DateTime.UtcNow.AddDays(1)
   };

   var ex = await Assert.ThrowsAsync<ArgumentException>(() => _service.UpdateAsync(1, dto, null));
   Assert.Equal("A data de início das licitações deve ser anterior à data de fim.", ex.Message);
  }

  [Fact]
  public async Task UpdateAsync_ShouldThrow_WhenPickupBeforeBiddingEnd()
  {
   var existing = new TransportRequest { Status = ERequestStatus.Draft };
   _repositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);

   var dto = new UpdateTransportRequestDTO
   {
    BiddingEndDate = DateTime.UtcNow.AddDays(2),
    PickupDate = DateTime.UtcNow.AddDays(1)
   };

   var ex = await Assert.ThrowsAsync<ArgumentException>(() => _service.UpdateAsync(1, dto, null));
   Assert.Equal("A data de recolha deve ser posterior ao fim das licitações.", ex.Message);
  }

  [Fact]
  public async Task UpdateAsync_ShouldThrow_WhenMaxPriceBelowMinimum()
  {
   var existing = new TransportRequest { Status = ERequestStatus.Draft };
   _repositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);

   var dto = new UpdateTransportRequestDTO
   {
    MaxPrice = 10
   };

   var ex = await Assert.ThrowsAsync<ArgumentException>(() => _service.UpdateAsync(1, dto, null));
   Assert.Equal("O preço deve ser igual ou superior a 20.", ex.Message);
  }

  [Fact]
  public async Task UpdateAsync_ShouldUpdateOnlyProvidedFields()
  {
   var existing = new TransportRequest
   {
    TransportRequestId = 1,
    Origin = "Lisboa",
    Destination = "Porto",
    Package = "Caixa antiga",
    Weight = 10,
    Volume = 15,
    Length = 50,
    Width = 30,
    Height = 25,
    Image = "img1.jpg",
    PickupDate = new DateTime(2025, 11, 1),
    DeliveryDate = new DateTime(2025, 11, 10),
    MaxPrice = 100,
    Status = ERequestStatus.Draft
   };

   var dto = new UpdateTransportRequestDTO
   {
    Destination = "Braga",
    Weight = 20,
    MaxPrice = 200
   };

   _repositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);
   _repositoryMock.Setup(r => r.UpdateAsync(1, It.IsAny<TransportRequest>()))
       .ReturnsAsync((int id, TransportRequest req) => req);

   var result = await _service.UpdateAsync(1, dto, null);

   Assert.NotNull(result);
   Assert.Equal("Lisboa", result.Origin);
   Assert.Equal("Braga", result.Destination);
   Assert.Equal(20, result.Weight);
   Assert.Equal(15, result.Volume);
   Assert.Equal(200, result.MaxPrice);
   _repositoryMock.Verify(r => r.UpdateAsync(1, It.IsAny<TransportRequest>()), Times.Once);
  }


  [Fact]
  public async Task UpdateAsync_ShouldThrow_WhenDimensionsAreInvalid()
  {
   var existing = new TransportRequest { Status = ERequestStatus.Draft };
   _repositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);

   var dto = new UpdateTransportRequestDTO
   {
    Length = 0, // inválido
    Width = 30,
    Height = 20
   };

   var ex = await Assert.ThrowsAsync<ArgumentException>(() => _service.UpdateAsync(1, dto, null));
   Assert.Equal("As dimensões devem ser superiores a zero.", ex.Message);
  }

  [Fact]
  public async Task UpdateAsync_ShouldThrow_WhenVolumeIsInvalid()
  {
   var existing = new TransportRequest { Status = ERequestStatus.Draft };
   _repositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);

   var dto = new UpdateTransportRequestDTO
   {
    Volume = 0 // inválido
   };

   var ex = await Assert.ThrowsAsync<ArgumentException>(() => _service.UpdateAsync(1, dto, null));
   Assert.Equal("O volume deve ser superior a zero.", ex.Message);
  }



  [Fact]
  public async Task DeleteAsync_ShouldThrow_WhenRequestNotFound()
  {
   _repositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
       .ReturnsAsync((TransportRequest?)null);

   await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.DeleteAsync(1));
  }

  [Fact]
  public async Task DeleteAsync_ShouldThrow_WhenStatusNotActive()
  {
   var existing = new TransportRequest { Status = ERequestStatus.Draft };
   _repositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);

   await Assert.ThrowsAsync<InvalidOperationException>(() => _service.DeleteAsync(1));
  }

  [Fact]
  public async Task DeleteAsync_ShouldReturnTrue_WhenValid()
  {
   var existing = new TransportRequest { Status = ERequestStatus.Active };
   _repositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);
   _repositoryMock.Setup(r => r.DeleteAsync(1)).ReturnsAsync(true);

   var result = await _service.DeleteAsync(1);

   Assert.True(result);
   _repositoryMock.Verify(r => r.DeleteAsync(1), Times.Once);
  }

  [Fact]
  public async Task GetByIdAsync_ShouldReturnRequest()
  {
   var expected = new TransportRequest { TransportRequestId = 5 };
   _repositoryMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(expected);

   var result = await _service.GetByIdAsync(5);

   Assert.NotNull(result);
   Assert.Equal(5, result.TransportRequestId);
  }

  [Fact]
  public async Task GetByCompanyAsync_ShouldReturnList()
  {
   var list = new List<TransportRequest>
   {
    new TransportRequest { TransportRequestId = 1, CompanyId = 2 },
    new TransportRequest { TransportRequestId = 2, CompanyId = 2 }
   };

   _repositoryMock.Setup(r => r.GetAllByCompanyAsync(2)).ReturnsAsync(list);

   var result = await _service.GetByCompanyAsync(2);

   Assert.Equal(2, result.Count);
   Assert.All(result, r => Assert.Equal(2, r.CompanyId));
  }
 }
}
