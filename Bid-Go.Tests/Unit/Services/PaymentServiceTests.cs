using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.DTOs;
using Bid_Go_Backend.Data.Models.Enums;
using Bid_Go_Backend.Repositories.Interfaces;
using Bid_Go_Backend.Services;
using Bid_Go_Backend.Services.Interfaces;
using Bid_Go_Backend.Services.Payments;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Bid_Go.Tests.Unit.Services
{
    /// <summary>
    /// Unit tests for PaymentService covering process, retry, and queries with gateway and repository interactions.
    /// </summary>
    public class PaymentServiceTests
    {
        private static CreatePaymentRequestDTO MakeCreateDto(int trId = 100, string token = "tok_test")
            => new CreatePaymentRequestDTO { TransportRequestId = trId, StripeToken = token };

        private static TransportRequest MakeTR(int trId = 100, int companyId = 200, int? selectedBidId = 300)
            => new TransportRequest
            {
                TransportRequestId = trId,
                CompanyId = companyId,
                SelectedBidId = selectedBidId
            };

        private static Bid MakeBid(int bidId = 300, int trId = 100, int driverId = 400, decimal value = 123.45m)
            => new Bid
            {
                BidId = bidId,
                TransportRequestId = trId,
                DriverId = driverId,
                Value = value
            };

        private static Payment MakePayment(int id = 1, int trId = 100, int companyId = 200, int driverId = 400, decimal gross = 123.45m)
            => new Payment
            {
                PaymentId = id,
                TransportRequestId = trId,
                CompanyId = companyId,
                DriverId = driverId,
                GrossValue = gross
            };

        [Fact]
        public async Task ProcessPayment_ShouldConfirm_AndSendNotifications_OnGatewaySuccess()
        {
            // Arrange
            var payments = new Mock<IPaymentRepository>();
            var bids = new Mock<IBidsService>();
            var trs = new Mock<ITransportRequestRepository>();
            var notifs = new Mock<INotificationService>();
            var gateway = new Mock<IPaymentGateway>();

            var tr = MakeTR();
            var bid = MakeBid(value: 200m); // valor para cálculo
            Payment? captured = null;

            trs.Setup(r => r.GetByIdAsync(tr.TransportRequestId)).ReturnsAsync(tr);
            bids.Setup(r => r.GetBidByIdAsync(tr.SelectedBidId!.Value)).ReturnsAsync(bid);

            payments.Setup(r => r.AddAsync(It.IsAny<Payment>()))
                    .Callback<Payment>(p => { p.PaymentId = 999; captured = p; })
                    .Returns(Task.CompletedTask);

            payments.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            gateway.Setup(g => g.ChargeAsync(
                    It.IsAny<long>(), "eur", It.IsAny<string>(),
                    It.Is<string>(d => d.Contains(tr.TransportRequestId.ToString())),
                    It.IsAny<IDictionary<string, string>>()));


            gateway.Setup(g => g.ChargeAsync(
                    It.IsAny<long>(), "eur", It.IsAny<string>(),
                    It.Is<string>(d => d.Contains(tr.TransportRequestId.ToString())),
                    It.IsAny<IDictionary<string, string>>()))
                   .ReturnsAsync(new ChargeResult(true, null));

            var sut = new PaymentService(payments.Object, bids.Object, trs.Object, notifs.Object, gateway.Object);

            // Act
            var dto = MakeCreateDto(tr.TransportRequestId, "tok_ok");
            var result = await sut.ProcessPaymentAsync(dto);

            // Assert
            Assert.NotNull(captured);
            Assert.Equal(EPaymentStatus.Confirmed, captured!.PaymentStatus);
            Assert.NotNull(captured.CompletedAt);
            Assert.Null(captured.FailureReason);

            // DTO mapeado
            Assert.Equal(captured.PaymentId, result.PaymentId);
            Assert.Equal(captured.GrossValue, result.GrossValue);
            Assert.Equal(captured.Tax, result.Tax);
            Assert.Equal(captured.NetValue, result.NetValue);
            Assert.Equal(EPaymentStatus.Confirmed, result.Status);

            // Interações
            trs.Verify(r => r.GetByIdAsync(tr.TransportRequestId), Times.Once);
            bids.Verify(r => r.GetBidByIdAsync(tr.SelectedBidId!.Value), Times.Once);
            payments.Verify(r => r.AddAsync(It.IsAny<Payment>()), Times.Once);
            payments.Verify(r => r.SaveChangesAsync(), Times.AtLeastOnce);
            gateway.VerifyAll();

            // Notificações enviadas
            // Notificação combinada
            notifs.Verify(n => n.CreateAndSendAsync(
                    tr.CompanyId,
                    It.Is<string>(s => s.Contains($"#{tr.TransportRequestId}")),
                    ENotificationType.Confirmed_Payment,
                    null,
                    tr.TransportRequestId),
                Times.Once);
        }

        [Fact]
        public async Task ProcessPayment_ShouldFailAndNotNotify_OnGatewayFailure()
        {
            // Arrange
            var payments = new Mock<IPaymentRepository>();
            var bids = new Mock<IBidsService>();
            var trs = new Mock<ITransportRequestRepository>();
            var notifs = new Mock<INotificationService>();
            var gateway = new Mock<IPaymentGateway>();

            var tr = MakeTR();
            var bid = MakeBid(value: 150m);
            Payment? captured = null;

            trs.Setup(r => r.GetByIdAsync(tr.TransportRequestId)).ReturnsAsync(tr);
            bids.Setup(r => r.GetBidByIdAsync(tr.SelectedBidId!.Value)).ReturnsAsync(bid);

            payments.Setup(r => r.AddAsync(It.IsAny<Payment>()))
                    .Callback<Payment>(p => captured = p)
                    .Returns(Task.CompletedTask);
            payments.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            gateway.Setup(g => g.ChargeAsync(
                    It.IsAny<long>(), "eur", It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<IDictionary<string, string>>()));


            gateway.Setup(g => g.ChargeAsync(
                    It.IsAny<long>(), "eur", It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<IDictionary<string, string>>() ))
                   .ReturnsAsync(new ChargeResult(false, "declined"));

            var sut = new PaymentService(payments.Object, bids.Object, trs.Object, notifs.Object, gateway.Object);

            // Act
            var result = await sut.ProcessPaymentAsync(MakeCreateDto());

            // Assert
            Assert.NotNull(captured);
            Assert.Equal(EPaymentStatus.Failed, captured!.PaymentStatus);
            Assert.Null(captured.CompletedAt);
            Assert.Equal("declined", captured.FailureReason);
            Assert.Equal(EPaymentStatus.Failed, result.Status);

            // Notificações NÃO enviadas
            notifs.Verify(n => n.CreateAndSendAsync(
                It.IsAny<int>(), It.IsAny<string>(), It.IsAny<ENotificationType>(), It.IsAny<int?>(), It.IsAny<int?>()), Times.Never);
        }

        [Fact]
        public async Task ProcessPayment_ShouldThrow_WhenTransportRequestNotFound()
        {
            // Arrange
            var payments = new Mock<IPaymentRepository>();
            var bids = new Mock<IBidsService>();
            var trs = new Mock<ITransportRequestRepository>();
            var notifs = new Mock<INotificationService>();
            var gateway = new Mock<IPaymentGateway>();

            trs.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((TransportRequest?)null);

            var sut = new PaymentService(payments.Object, bids.Object, trs.Object, notifs.Object, gateway.Object);

            // Act
            // Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => sut.ProcessPaymentAsync(MakeCreateDto()));
        }

        [Fact]
        public async Task ProcessPayment_ShouldThrow_WhenNoSelectedBid()
        {
            // Arrange
            var payments = new Mock<IPaymentRepository>();
            var bids = new Mock<IBidsService>();
            var trs = new Mock<ITransportRequestRepository>();
            var notifs = new Mock<INotificationService>();
            var gateway = new Mock<IPaymentGateway>();

            var tr = MakeTR(selectedBidId: null);
            trs.Setup(r => r.GetByIdAsync(tr.TransportRequestId)).ReturnsAsync(tr);

            var sut = new PaymentService(payments.Object, bids.Object, trs.Object, notifs.Object, gateway.Object);

            // Act
            // Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => sut.ProcessPaymentAsync(MakeCreateDto(tr.TransportRequestId)));
        }

        [Fact]
        public async Task ProcessPayment_ShouldThrow_WhenSelectedBidNotFound()
        {
            // Arrange
            var payments = new Mock<IPaymentRepository>();
            var bids = new Mock<IBidsService>();
            var trs = new Mock<ITransportRequestRepository>();
            var notifs = new Mock<INotificationService>();
            var gateway = new Mock<IPaymentGateway>();

            var tr = MakeTR();
            trs.Setup(r => r.GetByIdAsync(tr.TransportRequestId)).ReturnsAsync(tr);
            bids.Setup(r => r.GetBidByIdAsync(tr.SelectedBidId!.Value)).ReturnsAsync((Bid?)null);

            var sut = new PaymentService(payments.Object, bids.Object, trs.Object, notifs.Object, gateway.Object);

            // Act
            // Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => sut.ProcessPaymentAsync(MakeCreateDto(tr.TransportRequestId)));
        }

        [Fact]
        public async Task ProcessPayment_ShouldThrow_WhenSelectedBidDoesNotBelongToTR()
        {
            // Arrange
            var payments = new Mock<IPaymentRepository>();
            var bids = new Mock<IBidsService>();
            var trs = new Mock<ITransportRequestRepository>();
            var notifs = new Mock<INotificationService>();
            var gateway = new Mock<IPaymentGateway>();

            var tr = MakeTR(trId: 100);
            var bid = MakeBid(trId: 999); // bid de outro TR

            trs.Setup(r => r.GetByIdAsync(tr.TransportRequestId)).ReturnsAsync(tr);
            bids.Setup(r => r.GetBidByIdAsync(tr.SelectedBidId!.Value)).ReturnsAsync(bid);

            var sut = new PaymentService(payments.Object, bids.Object, trs.Object, notifs.Object, gateway.Object);

            // Act
            // Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => sut.ProcessPaymentAsync(MakeCreateDto(tr.TransportRequestId)));
        }

        [Fact]
        public async Task GetPaymentsByUserAsync_ShouldMapToDto()
        {
            // Arrange
            var payments = new Mock<IPaymentRepository>();
            var bids = new Mock<IBidsService>();
            var trs = new Mock<ITransportRequestRepository>();
            var notifs = new Mock<INotificationService>();
            var gateway = new Mock<IPaymentGateway>();

            var list = new List<Payment>
            {
                MakePayment(id:1, gross: 100m),
                MakePayment(id:2, gross: 200m),
            };

            payments.Setup(r => r.ListByUserAsync(777)).ReturnsAsync(list);

            var sut = new PaymentService(payments.Object, bids.Object, trs.Object, notifs.Object, gateway.Object);

            // Act
            var result = await sut.GetPaymentsByUserAsync(777);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Contains(result, p => p.PaymentId == 1 && p.GrossValue == 100m);
            Assert.Contains(result, p => p.PaymentId == 2 && p.GrossValue == 200m);
        }

        [Fact]
        public async Task RetryPayment_ShouldThrow_WhenPaymentNotFound()
        {
            // Arrange
            var payments = new Mock<IPaymentRepository>();
            var bids = new Mock<IBidsService>();
            var trs = new Mock<ITransportRequestRepository>();
            var notifs = new Mock<INotificationService>();
            var gateway = new Mock<IPaymentGateway>();

            payments.Setup(r => r.GetByIdForUpdateAsync(999)).ReturnsAsync((Payment?)null);

            var sut = new PaymentService(payments.Object, bids.Object, trs.Object, notifs.Object, gateway.Object);

            // Act
            // Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => sut.RetryPaymentAsync(999, "tok_retry"));
        }

        [Fact]
        public async Task RetryPayment_ShouldReturnFalse_WhenAlreadyConfirmed()
        {
            // Arrange
            var payments = new Mock<IPaymentRepository>();
            var bids = new Mock<IBidsService>();
            var trs = new Mock<ITransportRequestRepository>();
            var notifs = new Mock<INotificationService>();
            var gateway = new Mock<IPaymentGateway>();

            var payment = MakePayment();
            payment.PaymentStatus = EPaymentStatus.Confirmed;

            payments.Setup(r => r.GetByIdForUpdateAsync(payment.PaymentId)).ReturnsAsync(payment);

            var sut = new PaymentService(payments.Object, bids.Object, trs.Object, notifs.Object, gateway.Object);

            // Act
            var (ok, error, result) = await sut.RetryPaymentAsync(payment.PaymentId, "tok_retry");

            // Assert
            Assert.False(ok);
            Assert.Equal("This payment has already been completed.", error);
            Assert.Equal(EPaymentStatus.Confirmed, result!.Status);

            gateway.Verify(g => g.ChargeAsync(It.IsAny<long>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IDictionary<string, string>>()), Times.Never);
            notifs.Verify(n => n.CreateAndSendAsync(
                It.IsAny<int>(), It.IsAny<string>(), It.IsAny<ENotificationType>(), It.IsAny<int?>(), It.IsAny<int?>()), Times.Never);
        }

        [Fact]
        public async Task RetryPayment_ShouldReturnFalse_WhenDeadlinePassed()
        {
            // Arrange
            var payments = new Mock<IPaymentRepository>();
            var bids = new Mock<IBidsService>();
            var trs = new Mock<ITransportRequestRepository>();
            var notifs = new Mock<INotificationService>();
            var gateway = new Mock<IPaymentGateway>();

            var payment = MakePayment();
            payment.PaymentStatus = EPaymentStatus.Pending;
            payment.DeadlineToPay = DateTime.UtcNow.AddDays(-1);

            payments.Setup(r => r.GetByIdForUpdateAsync(payment.PaymentId)).ReturnsAsync(payment);
            payments.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            var sut = new PaymentService(payments.Object, bids.Object, trs.Object, notifs.Object, gateway.Object);

            // Act
            var (ok, error, result) = await sut.RetryPaymentAsync(payment.PaymentId, "tok_retry");

            // Assert
            Assert.False(ok);
            Assert.Equal("The payment deadline has passed. Please create a new payment.", error);
            Assert.Equal(EPaymentStatus.Pending, payment.PaymentStatus);
            Assert.Equal("The payment deadline has passed. Please create a new payment.", payment.FailureReason);
            payments.Verify(p => p.SaveChangesAsync(), Times.Once);

            gateway.Verify(g => g.ChargeAsync(It.IsAny<long>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IDictionary<string, string>>()), Times.Never);
            notifs.Verify(n => n.CreateAndSendAsync(
               It.IsAny<int>(), It.IsAny<string>(), It.IsAny<ENotificationType>(), It.IsAny<int?>(), It.IsAny<int?>()), Times.Never);
        }

        [Fact]
        public async Task RetryPayment_ShouldConfirm_AndNotify_OnGatewaySuccess()
        {
            // Arrange
            var payments = new Mock<IPaymentRepository>();
            var bids = new Mock<IBidsService>();
            var trs = new Mock<ITransportRequestRepository>();
            var notifs = new Mock<INotificationService>();
            var gateway = new Mock<IPaymentGateway>();

            var payment = MakePayment(trId: 100, companyId: 200);
            payment.PaymentStatus = EPaymentStatus.Pending;
            payment.DeadlineToPay = DateTime.UtcNow.AddDays(1);

            payments.Setup(r => r.GetByIdForUpdateAsync(payment.PaymentId)).ReturnsAsync(payment);
            payments.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            gateway.Setup(g => g.ChargeAsync(
                    It.IsAny<long>(), "eur", "tok_retry",
                    It.Is<string>(d => d.Contains("Retry payment")),
                    It.IsAny<IDictionary<string, string>>()))
                   .ReturnsAsync(new ChargeResult(true, null));


            var transportRequest = new TransportRequest { TransportRequestId = 100, Status = ERequestStatus.Pending };
            trs.Setup(r => r.GetByIdAsync(payment.TransportRequestId)).ReturnsAsync(transportRequest);

            var sut = new PaymentService(payments.Object, bids.Object, trs.Object, notifs.Object, gateway.Object);

            // Act
            var (ok, error, result) = await sut.RetryPaymentAsync(payment.PaymentId, "tok_retry");

            // Assert
            Assert.True(ok);
            Assert.Null(error);
            Assert.Equal(EPaymentStatus.Confirmed, payment.PaymentStatus);
            Assert.NotNull(payment.CompletedAt);
            Assert.Null(payment.FailureReason);
            payments.Verify(p => p.SaveChangesAsync(), Times.Once);

            notifs.Verify(n => n.CreateAndSendAsync(
                  payment.CompanyId,
                  It.Is<string>(s => s.Contains("successfully completed after retry")),
                  ENotificationType.Confirmed_Payment,
                  null,
                  payment.TransportRequestId),
                  Times.Once);
        }


        [Fact]
        public async Task RetryPayment_ShouldFail_AndNotNotify_OnGatewayFailure()
        {
            // Arrange
            var payments = new Mock<IPaymentRepository>();
            var bids = new Mock<IBidsService>();
            var trs = new Mock<ITransportRequestRepository>();
            var notifs = new Mock<INotificationService>();
            var gateway = new Mock<IPaymentGateway>();

            var payment = MakePayment();
            payment.PaymentStatus = EPaymentStatus.Pending;
            payment.DeadlineToPay = DateTime.UtcNow.AddDays(1);

            payments.Setup(r => r.GetByIdForUpdateAsync(payment.PaymentId)).ReturnsAsync(payment);
            payments.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            gateway.Setup(g => g.ChargeAsync(
                    It.IsAny<long>(), "eur", "tok_retry",
                    It.Is<string>(d => d.Contains("Retry payment")),
                    It.IsAny<IDictionary<string, string>>()))
                   .ReturnsAsync(new ChargeResult(false, "declined"));

            var transportRequest = new TransportRequest { TransportRequestId = 100, Status = ERequestStatus.Pending };
            trs.Setup(r => r.GetByIdAsync(payment.TransportRequestId)).ReturnsAsync(transportRequest);


            var sut = new PaymentService(payments.Object, bids.Object, trs.Object, notifs.Object, gateway.Object);

            // Act
            var (ok, error, result) = await sut.RetryPaymentAsync(payment.PaymentId, "tok_retry");

            // Assert
            Assert.False(ok);
            Assert.Equal("declined", error);
            Assert.Equal(EPaymentStatus.Failed, payment.PaymentStatus);
            Assert.Null(payment.CompletedAt);
            Assert.Equal("declined", payment.FailureReason);
            payments.Verify(p => p.SaveChangesAsync(), Times.Once);

            notifs.Verify(n => n.CreateAndSendAsync(
                 It.IsAny<int>(), It.IsAny<string>(), It.IsAny<ENotificationType>(), It.IsAny<int?>(), It.IsAny<int?>()), Times.Never);
        }
    }
}
