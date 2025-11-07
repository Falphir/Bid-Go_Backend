using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.Enums;
using Bid_Go_Backend.Repositories.Interfaces;
using Bid_Go_Backend.Services;
using Bid_Go_Backend.Services.Interfaces;
using Microsoft.Extensions.Logging;
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
 /// Unit tests for TransportUpdateStatusService validating transitions and side-effects.
 /// </summary>
 public class TransportUpdateStatusServiceTests
 {
  private readonly Mock<ITransportUpdateStatus> _mockRepo;
  private readonly Mock<INotificationService> _mockNotif;
  private readonly Mock<ILogger<TransportUpdateStatusService>> _mockLogger;
  private readonly Mock<IAuthorizationRepository> _mockAuthRepo;
  private readonly TransportUpdateStatusService _service;

  public TransportUpdateStatusServiceTests()
  {
   _mockRepo = new Mock<ITransportUpdateStatus>(MockBehavior.Strict);
   _mockNotif = new Mock<INotificationService>(MockBehavior.Strict);
   _mockLogger = new Mock<ILogger<TransportUpdateStatusService>>();
   _mockAuthRepo = new Mock<IAuthorizationRepository>(MockBehavior.Loose);
   _service = new TransportUpdateStatusService(_mockRepo.Object, _mockNotif.Object, _mockLogger.Object, _mockAuthRepo.Object);
  }

  private static ClaimsPrincipal MakeUser(int id, string role)
  {
   var claims = new List<Claim>
   {
    new Claim("userId", id.ToString()),
    new Claim("userType", role),
    new Claim(ClaimTypes.Role, role)
   };
   return new ClaimsPrincipal(new ClaimsIdentity(claims, "mock"));
  }

  private static TransportRequest MakeRequest(ERequestStatus status, List<Bid>? bids = null)
  {
   return new TransportRequest
   {
    TransportRequestId =1,
    Origin = "Lixa",
    Destination = "Felgueiras",
    Package = "Madeira",
    PickupDate = DateTime.UtcNow.AddDays(-2),
    DeliveryDate = DateTime.UtcNow.AddDays(-1),
    Weight =1,
    Volume =1,
    Length =1,
    Width =1,
    Height =1,
    Image = "img",
    MaxPrice =100,
    Status = status,
    BiddingStartDate = DateTime.UtcNow.AddDays(-10),
    BiddingEndDate = DateTime.UtcNow.AddDays(-5),
    IsAutomaticSelectionEnabled = false,
    CompanyId =100,
    Bids = bids ?? new List<Bid>()
   };
  }

  private static Company MakeCompany(int id =100) => new Company { Id = id, Name = "Co", Email = "c@x.com", Password = "p", PhoneNumber =123456789, CompanyName = "C1", Address = "A" };
  private static Driver MakeDriver(int id =200) => new Driver { Id = id, Name = "Dr", Email = "d@x.com", Password = "p", PhoneNumber =123456789, DriverLicense = "DL", Insurance = "IN" };

  [Fact]
  public async Task UpdateRequestStatusAsync_Returns404_WhenRequestNotFound()
  {
   // Arrange
   _mockRepo.Setup(r => r.GetTransportRequestWithBidsAsync(1)).ReturnsAsync((TransportRequest?)null);

   // Act
   var result = await _service.UpdateRequestStatusAsync(1, MakeUser(100, "Company"), ERequestStatus.Pending);

   // Assert
   Assert.Equal(404, result.StatusCode);
   _mockRepo.Verify(r => r.GetTransportRequestWithBidsAsync(1), Times.Once);
   _mockRepo.VerifyNoOtherCalls();
   _mockNotif.VerifyNoOtherCalls();
  }

  [Fact]
  public async Task UpdateRequestStatusAsync_Returns401_WhenTokenInvalid()
  {
   // user without required claims
   var invalidUser = new ClaimsPrincipal(new ClaimsIdentity());
   // service should return401 before consulting repo
   var result = await _service.UpdateRequestStatusAsync(1, invalidUser, ERequestStatus.Pending);

   Assert.Equal(401, result.StatusCode);
   _mockRepo.Verify(r => r.GetTransportRequestWithBidsAsync(It.IsAny<int>()), Times.Never);
   _mockRepo.VerifyNoOtherCalls();
   _mockNotif.VerifyNoOtherCalls();
  }

  [Fact]
  public async Task Company_ActiveToPending_UpdatesStatus_NoNotifications()
  {
   var bids = new List<Bid>
   {
    new Bid { BidId =1, DriverId =10, TransportRequestId =1, Status = EBidStatus.Pendent, DeliveryDeadline = DateTime.UtcNow.AddDays(1), Value =10 },
    new Bid { BidId =2, DriverId =11, TransportRequestId =1, Status = EBidStatus.Accepted, DeliveryDeadline = DateTime.UtcNow.AddDays(1), Value =12 }
   };
   var req = MakeRequest(ERequestStatus.Active, bids);

   _mockRepo.Setup(r => r.GetTransportRequestWithBidsAsync(1)).ReturnsAsync(req);
   _mockRepo.Setup(r => r.UpdateTransportRequest(It.IsAny<TransportRequest>()));
   _mockRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

   var result = await _service.UpdateRequestStatusAsync(1, MakeUser(100, "Company"), ERequestStatus.Pending);

   Assert.Equal(200, result.StatusCode);
   _mockRepo.Verify(r => r.UpdateTransportRequest(It.Is<TransportRequest>(t => t.Status == ERequestStatus.Pending)), Times.Once);
   _mockRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
   _mockNotif.VerifyNoOtherCalls();
   _mockRepo.Verify(r => r.GetTransportRequestWithBidsAsync(1), Times.Once);
   _mockRepo.VerifyNoOtherCalls();
  }

  [Fact]
  public async Task Company_ActiveToCanceled_CancelsPendingBids_SendsNotifications_UpdatesBids()
  {
   var bids = new List<Bid>
   {
    new Bid { BidId =1, DriverId =10, TransportRequestId =1, Status = EBidStatus.Pendent, DeliveryDeadline = DateTime.UtcNow.AddDays(1), Value =10 },
    new Bid { BidId =2, DriverId =11, TransportRequestId =1, Status = EBidStatus.Accepted, DeliveryDeadline = DateTime.UtcNow.AddDays(1), Value =12 },
    new Bid { BidId =3, DriverId =12, TransportRequestId =1, Status = EBidStatus.Pendent, DeliveryDeadline = DateTime.UtcNow.AddDays(1), Value =13 }
   };
   var req = MakeRequest(ERequestStatus.Active, bids);

   _mockRepo.Setup(r => r.GetTransportRequestWithBidsAsync(1)).ReturnsAsync(req);
   _mockRepo.Setup(r => r.UpdateTransportRequest(It.IsAny<TransportRequest>()));
   _mockRepo.Setup(r => r.UpdateBids(It.IsAny<IEnumerable<Bid>>()));
   _mockRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

   _mockNotif.Setup(n => n.CreateAndSendAsync(
        It.IsAny<int>(),
        It.IsAny<string>(),
        ENotificationType.Canceled,
        It.IsAny<int?>(),
        It.IsAny<int?>()))
    .ReturnsAsync(new Notification());

   var result = await _service.UpdateRequestStatusAsync(1, MakeUser(100, "Company"), ERequestStatus.Canceled);

   Assert.Equal(200, result.StatusCode);

   var pendingCanceled = bids.Where(b => b.BidId ==1 || b.BidId ==3).ToList();
   Assert.All(pendingCanceled, b => Assert.Equal(EBidStatus.Canceled, b.Status));

   _mockRepo.Verify(r => r.UpdateBids(It.Is<IEnumerable<Bid>>(bs => bs.All(b => (b.BidId ==1 || b.BidId ==3) && b.Status == EBidStatus.Canceled) && bs.Count() ==2)), Times.Once);
   _mockRepo.Verify(r => r.UpdateTransportRequest(It.Is<TransportRequest>(t => t.Status == ERequestStatus.Canceled)), Times.Once);
   _mockRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
   _mockRepo.Verify(r => r.GetTransportRequestWithBidsAsync(1), Times.Once);
   _mockRepo.VerifyNoOtherCalls();
  }

  [Fact]
  public async Task Company_InvalidTransition_Returns400()
  {
   var req = MakeRequest(ERequestStatus.Active);
   _mockRepo.Setup(r => r.GetTransportRequestWithBidsAsync(1)).ReturnsAsync(req);

   var result = await _service.UpdateRequestStatusAsync(1, MakeUser(100, "Company"), ERequestStatus.InTransit);

   Assert.Equal(400, result.StatusCode);
   _mockRepo.Verify(r => r.GetTransportRequestWithBidsAsync(1), Times.Once);
   _mockRepo.VerifyNoOtherCalls();
   _mockNotif.VerifyNoOtherCalls();
  }

  [Fact]
  public async Task Driver_WaitingPickupToInTransit_UpdatesStatus()
  {
   var req = MakeRequest(ERequestStatus.WaitingPickup);
   req.SelectedBid = new Bid { BidId =99, DriverId =200, TransportRequestId =1, Status = EBidStatus.Accepted };

   _mockRepo.Setup(r => r.GetTransportRequestWithBidsAsync(1)).ReturnsAsync(req);
   _mockRepo.Setup(r => r.UpdateTransportRequest(It.IsAny<TransportRequest>()));
   _mockRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

   var result = await _service.UpdateRequestStatusAsync(1, MakeUser(200, "Driver"), ERequestStatus.InTransit);

   Assert.Equal(200, result.StatusCode);
   _mockRepo.Verify(r => r.UpdateTransportRequest(It.Is<TransportRequest>(t => t.Status == ERequestStatus.InTransit)), Times.Once);
   _mockRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
   _mockRepo.Verify(r => r.GetTransportRequestWithBidsAsync(1), Times.Once);
   _mockRepo.VerifyNoOtherCalls();
   _mockNotif.VerifyNoOtherCalls();
  }

  [Fact]
  public async Task Driver_InTransitToCompleted_UpdatesStatus()
  {
   var req = MakeRequest(ERequestStatus.InTransit);
   req.SelectedBid = new Bid { BidId =100, DriverId =200, TransportRequestId =1, Status = EBidStatus.Accepted };

   _mockRepo.Setup(r => r.GetTransportRequestWithBidsAsync(1)).ReturnsAsync(req);
   _mockRepo.Setup(r => r.UpdateTransportRequest(It.IsAny<TransportRequest>()));
   _mockRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

   var result = await _service.UpdateRequestStatusAsync(1, MakeUser(200, "Driver"), ERequestStatus.Completed);

   Assert.Equal(200, result.StatusCode);
   _mockRepo.Verify(r => r.UpdateTransportRequest(It.Is<TransportRequest>(t => t.Status == ERequestStatus.Completed)), Times.Once);
   _mockRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
   _mockRepo.Verify(r => r.GetTransportRequestWithBidsAsync(1), Times.Once);
   _mockRepo.VerifyNoOtherCalls();
   _mockNotif.VerifyNoOtherCalls();
  }

  [Fact]
  public async Task Driver_InvalidTransition_Returns400()
  {
   var req = MakeRequest(ERequestStatus.Pending);
   req.SelectedBid = new Bid { BidId =101, DriverId =200, TransportRequestId =1, Status = EBidStatus.Accepted };
   _mockRepo.Setup(r => r.GetTransportRequestWithBidsAsync(1)).ReturnsAsync(req);

   var result = await _service.UpdateRequestStatusAsync(1, MakeUser(200, "Driver"), ERequestStatus.WaitingPickup);

   Assert.Equal(400, result.StatusCode);
   _mockRepo.Verify(r => r.GetTransportRequestWithBidsAsync(1), Times.Once);
   _mockRepo.VerifyNoOtherCalls();
   _mockNotif.VerifyNoOtherCalls();
  }

  [Fact]
  public async Task Driver_InTransitToCanceled_NoNotifications()
  {
   var req = MakeRequest(ERequestStatus.InTransit, new List<Bid>
   {
    new Bid { BidId =1, DriverId =10, TransportRequestId =1, Status = EBidStatus.Pendent, DeliveryDeadline = DateTime.UtcNow.AddDays(1), Value =10 },
   });
   req.SelectedBid = new Bid { BidId =102, DriverId =200, TransportRequestId =1, Status = EBidStatus.Accepted };

   _mockRepo.Setup(r => r.GetTransportRequestWithBidsAsync(1)).ReturnsAsync(req);
   _mockRepo.Setup(r => r.UpdateTransportRequest(It.IsAny<TransportRequest>()));
   _mockRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

   var result = await _service.UpdateRequestStatusAsync(1, MakeUser(200, "Driver"), ERequestStatus.Canceled);

   Assert.Equal(200, result.StatusCode);
   _mockRepo.Verify(r => r.UpdateTransportRequest(It.Is<TransportRequest>(t => t.Status == ERequestStatus.Canceled)), Times.Once);
   _mockRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
   _mockRepo.Verify(r => r.GetTransportRequestWithBidsAsync(1), Times.Once);
   _mockRepo.VerifyNoOtherCalls();
   _mockNotif.VerifyNoOtherCalls();
  }
 }
}
