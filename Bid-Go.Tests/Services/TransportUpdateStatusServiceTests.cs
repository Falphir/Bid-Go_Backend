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
using System.Threading.Tasks;
using Xunit;

namespace Bid_Go_Backend.Tests.Services
{
    public class TransportUpdateStatusServiceTests
    {
        private readonly Mock<ITransportUpdateStatus> _mockRepo;
        private readonly Mock<INotificationService> _mockNotif;
        private readonly Mock<ILogger<TransportUpdateStatusService>> _mockLogger;
        private readonly TransportUpdateStatusService _service;

        public TransportUpdateStatusServiceTests()
        {
            _mockRepo = new Mock<ITransportUpdateStatus>(MockBehavior.Strict);
            _mockNotif = new Mock<INotificationService>(MockBehavior.Strict);
            _mockLogger = new Mock<ILogger<TransportUpdateStatusService>>();
            _service = new TransportUpdateStatusService(_mockRepo.Object, _mockNotif.Object, _mockLogger.Object);
        }

        private static TransportRequest MakeRequest(ERequestStatus status, List<Bid>? bids = null)
        {
            return new TransportRequest
            {
                TransportRequestId = 1,
                Origin = "Lixa",
                Destination = "Felgueiras",
                Package = "Madeira",
                PickupDate = DateTime.UtcNow.AddDays(-2),
                DeliveryDate = DateTime.UtcNow.AddDays(-1),
                Weight = 1,
                Volume = 1,
                Length = 1,
                Width = 1,
                Height = 1,
                Image = "img",
                MaxPrice = 100,
                Status = status,
                BiddingStartDate = DateTime.UtcNow.AddDays(-10),
                BiddingEndDate = DateTime.UtcNow.AddDays(-5),
                IsAutomaticSelectionEnabled = false,
                CompanyId = 100,
                Bids = bids ?? new List<Bid>()
            };
        }

        private static Company MakeCompany(int id = 100) => new Company { Id = id, Name = "Co", Email = "c@x.com", Password = "p", PhoneNumber = 123456789, CompanyName = "C1", Address = "A" };
        private static Driver MakeDriver(int id = 200) => new Driver { Id = id, Name = "Dr", Email = "d@x.com", Password = "p", PhoneNumber = 123456789, DriverLicense = "DL", Insurance = "IN" };

        [Fact]
        public async Task UpdateRequestStatusAsync_ReturnsNull_WhenRequestNotFound()
        {
            _mockRepo.Setup(r => r.GetTransportRequestWithBidsAsync(1)).ReturnsAsync((TransportRequest?)null);

            var result = await _service.UpdateRequestStatusAsync(1, 100, ERequestStatus.Pending);

            Assert.Null(result);
            _mockRepo.Verify(r => r.GetTransportRequestWithBidsAsync(1), Times.Once);
            _mockRepo.VerifyNoOtherCalls();
            _mockNotif.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task UpdateRequestStatusAsync_Throws_WhenUserNotFound()
        {
            var req = MakeRequest(ERequestStatus.Active);
            _mockRepo.Setup(r => r.GetTransportRequestWithBidsAsync(1)).ReturnsAsync(req);
            _mockRepo.Setup(r => r.GetUserByIdAsync(999)).ReturnsAsync((User?)null);

            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.UpdateRequestStatusAsync(1, 999, ERequestStatus.Pending));

            _mockRepo.Verify(r => r.GetTransportRequestWithBidsAsync(1), Times.Once);
            _mockRepo.Verify(r => r.GetUserByIdAsync(999), Times.Once);
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
            var co = MakeCompany(100);

            _mockRepo.Setup(r => r.GetTransportRequestWithBidsAsync(1)).ReturnsAsync(req);
            _mockRepo.Setup(r => r.GetUserByIdAsync(100)).ReturnsAsync(co);
            _mockRepo.Setup(r => r.UpdateTransportRequest(It.IsAny<TransportRequest>()));
            _mockRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            var result = await _service.UpdateRequestStatusAsync(1, 100, ERequestStatus.Pending);

            Assert.NotNull(result);
            Assert.Equal(ERequestStatus.Pending, result.Status);
            // bids unchanged, and no notifications or UpdateBids called
            Assert.Equal(EBidStatus.Pendent, bids.First(b => b.BidId == 1).Status);
            _mockRepo.Verify(r => r.UpdateTransportRequest(It.Is<TransportRequest>(t => t.Status == ERequestStatus.Pending)), Times.Once);
            _mockRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
            _mockNotif.VerifyNoOtherCalls();
            _mockRepo.Verify(r => r.GetTransportRequestWithBidsAsync(1), Times.Once);
            _mockRepo.Verify(r => r.GetUserByIdAsync(100), Times.Once);
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
            var co = MakeCompany(100);

            _mockRepo.Setup(r => r.GetTransportRequestWithBidsAsync(1)).ReturnsAsync(req);
            _mockRepo.Setup(r => r.GetUserByIdAsync(100)).ReturnsAsync(co);
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


            var result = await _service.UpdateRequestStatusAsync(1, 100, ERequestStatus.Canceled);

            Assert.NotNull(result);
            Assert.Equal(ERequestStatus.Canceled, result.Status);

            // pending bids should be canceled in-memory and UpdateBids called with them
            var pendingCanceled = bids.Where(b => b.BidId is 1 or 3).ToList();
            Assert.All(pendingCanceled, b => Assert.Equal(EBidStatus.Canceled, b.Status));

            _mockRepo.Verify(r => r.UpdateBids(It.Is<IEnumerable<Bid>>(bs => bs.All(b => (b.BidId == 1 || b.BidId == 3) && b.Status == EBidStatus.Canceled) && bs.Count() == 2)), Times.Once);

            // notifications sent for each pending bid
            _mockNotif.Verify(n => n.CreateAndSendAsync(
                10,
                It.IsAny<string>(),
                ENotificationType.Canceled,
                1,
                1), Times.Once);

            _mockNotif.Verify(n => n.CreateAndSendAsync(
                12,
                It.IsAny<string>(),
                ENotificationType.Canceled,
                3,
                1), Times.Once);


            _mockRepo.Verify(r => r.UpdateTransportRequest(It.Is<TransportRequest>(t => t.Status == ERequestStatus.Canceled)), Times.Once);
            _mockRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
            _mockRepo.Verify(r => r.GetTransportRequestWithBidsAsync(1), Times.Once);
            _mockRepo.Verify(r => r.GetUserByIdAsync(100), Times.Once);
            _mockRepo.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Company_InvalidTransition_Throws()
        {
            var req = MakeRequest(ERequestStatus.Active);
            var co = MakeCompany(100);

            _mockRepo.Setup(r => r.GetTransportRequestWithBidsAsync(1)).ReturnsAsync(req);
            _mockRepo.Setup(r => r.GetUserByIdAsync(100)).ReturnsAsync(co);

            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.UpdateRequestStatusAsync(1, 100, ERequestStatus.InTransit));

            _mockRepo.Verify(r => r.GetTransportRequestWithBidsAsync(1), Times.Once);
            _mockRepo.Verify(r => r.GetUserByIdAsync(100), Times.Once);
            _mockRepo.VerifyNoOtherCalls();
            _mockNotif.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Driver_WaitingPickupToInTransit_UpdatesStatus()
        {
            var req = MakeRequest(ERequestStatus.WaitingPickup);
            var dr = MakeDriver(200);

            _mockRepo.Setup(r => r.GetTransportRequestWithBidsAsync(1)).ReturnsAsync(req);
            _mockRepo.Setup(r => r.GetUserByIdAsync(200)).ReturnsAsync(dr);
            _mockRepo.Setup(r => r.UpdateTransportRequest(It.IsAny<TransportRequest>()));
            _mockRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            var result = await _service.UpdateRequestStatusAsync(1, 200, ERequestStatus.InTransit);

            Assert.NotNull(result);
            Assert.Equal(ERequestStatus.InTransit, result.Status);
            _mockRepo.Verify(r => r.UpdateTransportRequest(It.Is<TransportRequest>(t => t.Status == ERequestStatus.InTransit)), Times.Once);
            _mockRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
            _mockRepo.Verify(r => r.GetTransportRequestWithBidsAsync(1), Times.Once);
            _mockRepo.Verify(r => r.GetUserByIdAsync(200), Times.Once);
            _mockRepo.VerifyNoOtherCalls();
            _mockNotif.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Driver_InTransitToCompleted_UpdatesStatus()
        {
            var req = MakeRequest(ERequestStatus.InTransit);
            var dr = MakeDriver(200);

            _mockRepo.Setup(r => r.GetTransportRequestWithBidsAsync(1)).ReturnsAsync(req);
            _mockRepo.Setup(r => r.GetUserByIdAsync(200)).ReturnsAsync(dr);
            _mockRepo.Setup(r => r.UpdateTransportRequest(It.IsAny<TransportRequest>()));
            _mockRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            var result = await _service.UpdateRequestStatusAsync(1, 200, ERequestStatus.Completed);

            Assert.NotNull(result);
            Assert.Equal(ERequestStatus.Completed, result.Status);
            _mockRepo.Verify(r => r.UpdateTransportRequest(It.Is<TransportRequest>(t => t.Status == ERequestStatus.Completed)), Times.Once);
            _mockRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
            _mockRepo.Verify(r => r.GetTransportRequestWithBidsAsync(1), Times.Once);
            _mockRepo.Verify(r => r.GetUserByIdAsync(200), Times.Once);
            _mockRepo.VerifyNoOtherCalls();
            _mockNotif.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Driver_InvalidTransition_Throws()
        {
            var req = MakeRequest(ERequestStatus.Pending);
            var dr = MakeDriver(200);

            _mockRepo.Setup(r => r.GetTransportRequestWithBidsAsync(1)).ReturnsAsync(req);
            _mockRepo.Setup(r => r.GetUserByIdAsync(200)).ReturnsAsync(dr);

            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.UpdateRequestStatusAsync(1, 200, ERequestStatus.WaitingPickup));

            _mockRepo.Verify(r => r.GetTransportRequestWithBidsAsync(1), Times.Once);
            _mockRepo.Verify(r => r.GetUserByIdAsync(200), Times.Once);
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
            var dr = MakeDriver(200);

            _mockRepo.Setup(r => r.GetTransportRequestWithBidsAsync(1)).ReturnsAsync(req);
            _mockRepo.Setup(r => r.GetUserByIdAsync(200)).ReturnsAsync(dr);
            _mockRepo.Setup(r => r.UpdateTransportRequest(It.IsAny<TransportRequest>()));
            _mockRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            var result = await _service.UpdateRequestStatusAsync(1, 200, ERequestStatus.Canceled);

            Assert.NotNull(result);
            Assert.Equal(ERequestStatus.Canceled, result.Status);
            _mockNotif.VerifyNoOtherCalls();
            _mockRepo.Verify(r => r.UpdateTransportRequest(It.Is<TransportRequest>(t => t.Status == ERequestStatus.Canceled)), Times.Once);
            _mockRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
            _mockRepo.Verify(r => r.GetTransportRequestWithBidsAsync(1), Times.Once);
            _mockRepo.Verify(r => r.GetUserByIdAsync(200), Times.Once);
            _mockRepo.VerifyNoOtherCalls();
        }
    }
}
