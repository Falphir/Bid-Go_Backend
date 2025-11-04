using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bid_Go_Backend.Controllers;
using Bid_Go_Backend.Data.Models.DTOs;
using Bid_Go_Backend.Data.Models.Enums;
using Bid_Go_Backend.Data.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Bid_Go.Tests.Controllers
{
    public class PaymentsControllerTests
    {
        private readonly Mock<IPaymentRepository> _mockRepo;
        private readonly PaymentsController _controller;

        public PaymentsControllerTests()
        {
            _mockRepo = new Mock<IPaymentRepository>();
            _controller = new PaymentsController(_mockRepo.Object);
        }

        [Fact]
        public async Task ProcessPayment_ShouldReturnBadRequest_WhenPaymentFailed()
        {
            var dto = new CreatePaymentRequestDTO { BidId =1, StripeToken = "tok_invalid" };
            var resultDto = new PaymentResultDTO { PaymentId =1, GrossValue =100, Tax =5, NetValue =95, Status = EPaymentStatus.Failed, FailureReason = "Card declined", CreatedAt = DateTime.UtcNow };

            _mockRepo.Setup(r => r.ProcessPaymentAsync(dto)).ReturnsAsync(resultDto);

            var result = await _controller.ProcessPayment(dto);

            var bad = Assert.IsType<BadRequestObjectResult>(result);
            var val = bad.Value;
            var prop = val.GetType().GetProperty("message");
            Assert.NotNull(prop);
            Assert.Equal("Payment was declined by the payment gateway.", prop.GetValue(val));

            var paymentProp = val.GetType().GetProperty("payment");
            Assert.NotNull(paymentProp);
        }

        [Fact]
        public async Task ProcessPayment_ShouldReturnOk_WhenPaymentSucceeded()
        {
            var dto = new CreatePaymentRequestDTO { BidId =2, StripeToken = "tok_ok" };
            var resultDto = new PaymentResultDTO { PaymentId =2, GrossValue =100, Tax =5, NetValue =95, Status = EPaymentStatus.Confirmed, CreatedAt = DateTime.UtcNow };

            _mockRepo.Setup(r => r.ProcessPaymentAsync(dto)).ReturnsAsync(resultDto);

            var result = await _controller.ProcessPayment(dto);

            var ok = Assert.IsType<OkObjectResult>(result);
            var val = ok.Value;
            var prop = val.GetType().GetProperty("message");
            Assert.NotNull(prop);
            Assert.Equal("Payment successfully processed.", prop.GetValue(val));

            var paymentProp = val.GetType().GetProperty("payment");
            Assert.NotNull(paymentProp);
        }

        [Fact]
        public async Task GetPaymentsByUser_ShouldReturnOkWithList()
        {
            var list = new List<PaymentResultDTO>
            {
                new PaymentResultDTO { PaymentId =1, GrossValue =50, Tax =2.5m, NetValue =47.5m, Status = EPaymentStatus.Confirmed, CreatedAt = DateTime.UtcNow },
                new PaymentResultDTO { PaymentId =2, GrossValue =75, Tax =3.75m, NetValue =71.25m, Status = EPaymentStatus.Failed, CreatedAt = DateTime.UtcNow }
            };

            _mockRepo.Setup(r => r.GetPaymentsByUserAsync(10)).ReturnsAsync(list);

            var result = await _controller.GetPaymentsByUser(10);

            var ok = Assert.IsType<OkObjectResult>(result);
            var val = Assert.IsAssignableFrom<IEnumerable<PaymentResultDTO>>(ok.Value);
            Assert.Equal(2, val.Count());
        }

        [Fact]
        public async Task Retry_ShouldReturnOk_WhenRetrySucceeds()
        {
            var paymentId =5;
            var dto = new RetryPaymentRequestDTO { StripeToken = "tok_ok" };
            var resultDto = new PaymentResultDTO { PaymentId = paymentId, GrossValue =100, Tax =5, NetValue =95, Status = EPaymentStatus.Confirmed, CreatedAt = DateTime.UtcNow };

            _mockRepo.Setup(r => r.RetryPaymentAsync(paymentId, dto.StripeToken)).ReturnsAsync(resultDto);

            var result = await _controller.Retry(paymentId, dto);

            var ok = Assert.IsType<OkObjectResult>(result);
            var val = ok.Value;
            var prop = val.GetType().GetProperty("message");
            Assert.NotNull(prop);
            Assert.Equal("Payment completed on retry.", prop.GetValue(val));
        }

        [Fact]
        public async Task Retry_ShouldReturnBadRequest_WhenRetryNotCompleted()
        {
            var paymentId =6;
            var dto = new RetryPaymentRequestDTO { StripeToken = "tok_fail" };
            var resultDto = new PaymentResultDTO { PaymentId = paymentId, GrossValue =100, Tax =5, NetValue =95, Status = EPaymentStatus.Failed, CreatedAt = DateTime.UtcNow };

            _mockRepo.Setup(r => r.RetryPaymentAsync(paymentId, dto.StripeToken)).ReturnsAsync(resultDto);

            var result = await _controller.Retry(paymentId, dto);

            var bad = Assert.IsType<BadRequestObjectResult>(result);
            var val = bad.Value;
            var prop = val.GetType().GetProperty("message");
            Assert.NotNull(prop);
            Assert.Equal("Retry was executed, but the payment was not completed.", prop.GetValue(val));
        }

        [Fact]
        public async Task Retry_ShouldReturnBadRequest_WhenRepositoryThrowsInvalidOperation()
        {
            var paymentId =7;
            var dto = new RetryPaymentRequestDTO { StripeToken = "tok_fail" };

            _mockRepo.Setup(r => r.RetryPaymentAsync(paymentId, dto.StripeToken)).ThrowsAsync(new InvalidOperationException("This payment has already been completed."));

            var result = await _controller.Retry(paymentId, dto);

            var bad = Assert.IsType<BadRequestObjectResult>(result);
            var val = bad.Value;
            var prop = val.GetType().GetProperty("message");
            Assert.NotNull(prop);
            Assert.Equal("This payment has already been completed.", prop.GetValue(val));
        }
    }
}
