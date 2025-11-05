using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Repositories.Interfaces;
using Bid_Go_Backend.Services;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Bid_Go_Backend.Tests.Services
{
    public class AuthorizationServiceTests
    {
        private readonly Mock<IAuthorizationRepository> _repoMock;
        private readonly AuthorizationService _service;

        public AuthorizationServiceTests()
        {
            _repoMock = new Mock<IAuthorizationRepository>();
            _service = new AuthorizationService(_repoMock.Object);
        }


        [Fact]
        public async Task CompanyOwnsTransportRequestAsync_ShouldReturnTrue_WhenCompanyOwnsRequest()
        {
            var request = new TransportRequest { TransportRequestId = 1, CompanyId = 5 };
            _repoMock.Setup(r => r.GetTransportRequestAsync(1)).ReturnsAsync(request);

            var result = await _service.CompanyOwnsTransportRequestAsync(5, 1);

            Assert.True(result);
        }

        [Fact]
        public async Task CompanyOwnsTransportRequestAsync_ShouldReturnFalse_WhenCompanyDoesNotOwnRequest()
        {
            var request = new TransportRequest { TransportRequestId = 1, CompanyId = 10 };
            _repoMock.Setup(r => r.GetTransportRequestAsync(1)).ReturnsAsync(request);

            var result = await _service.CompanyOwnsTransportRequestAsync(5, 1);

            Assert.False(result);
        }


       
        [Fact]
        public async Task DriverOwnsBidAsync_ShouldReturnTrue_WhenDriverOwnsBid()
        {
            var bid = new Bid { BidId = 1, DriverId = 2 };
            _repoMock.Setup(r => r.GetBidAsync(1)).ReturnsAsync(bid);

            var result = await _service.DriverOwnsBidAsync(2, 1);

            Assert.True(result);
        }

        [Fact]
        public async Task DriverOwnsBidAsync_ShouldReturnFalse_WhenDriverDoesNotOwnBid()
        {
            var bid = new Bid { BidId = 1, DriverId = 5 };
            _repoMock.Setup(r => r.GetBidAsync(1)).ReturnsAsync(bid);

            var result = await _service.DriverOwnsBidAsync(2, 1);

            Assert.False(result);
        }

   
        [Fact]
        public async Task CompanyOwnsPaymentAsync_ShouldReturnTrue_WhenCompanyOwnsPayment()
        {
            var payment = new Payment
            {
                PaymentId = 1,
                TransportRequest = new TransportRequest { CompanyId = 5 }
            };
            _repoMock.Setup(r => r.GetPaymentAsync(1)).ReturnsAsync(payment);

            var result = await _service.CompanyOwnsPaymentAsync(5, 1);

            Assert.True(result);
        }

        [Fact]
        public async Task CompanyOwnsPaymentAsync_ShouldReturnFalse_WhenCompanyDoesNotOwnPayment()
        {
            var payment = new Payment
            {
                PaymentId = 1,
                TransportRequest = new TransportRequest { CompanyId = 9 }
            };
            _repoMock.Setup(r => r.GetPaymentAsync(1)).ReturnsAsync(payment);

            var result = await _service.CompanyOwnsPaymentAsync(5, 1);

            Assert.False(result);
        }

        [Fact]
        public async Task CompanyOwnsPaymentAsync_ShouldReturnFalse_WhenPaymentNotFound()
        {
            _repoMock.Setup(r => r.GetPaymentAsync(It.IsAny<int>())).ReturnsAsync((Payment?)null);

            var result = await _service.CompanyOwnsPaymentAsync(5, 99);

            Assert.False(result);
        }

 
        [Fact]
        public async Task UserOwnsChatAsync_ShouldReturnTrue_WhenUserIsCompany()
        {
            var chat = new Chats
            {
                ChatId = 1,
                TransportRequest = new TransportRequest
                {
                    CompanyId = 10,
                    SelectedBid = new Bid { DriverId = 20 }
                }
            };
            _repoMock.Setup(r => r.GetChatWithRelationsAsync(1)).ReturnsAsync(chat);

            var result = await _service.UserOwnsChatAsync(10, 1);

            Assert.True(result);
        }

        [Fact]
        public async Task UserOwnsChatAsync_ShouldReturnTrue_WhenUserIsDriver()
        {
            var chat = new Chats
            {
                ChatId = 1,
                TransportRequest = new TransportRequest
                {
                    CompanyId = 10,
                    SelectedBid = new Bid { DriverId = 20 }
                }
            };
            _repoMock.Setup(r => r.GetChatWithRelationsAsync(1)).ReturnsAsync(chat);

            var result = await _service.UserOwnsChatAsync(20, 1);

            Assert.True(result);
        }

        [Fact]
        public async Task UserOwnsChatAsync_ShouldReturnFalse_WhenUserNotCompanyOrDriver()
        {
            var chat = new Chats
            {
                ChatId = 1,
                TransportRequest = new TransportRequest
                {
                    CompanyId = 10,
                    SelectedBid = new Bid { DriverId = 20 }
                }
            };
            _repoMock.Setup(r => r.GetChatWithRelationsAsync(1)).ReturnsAsync(chat);

            var result = await _service.UserOwnsChatAsync(99, 1);

            Assert.False(result);
        }

        [Fact]
        public async Task UserOwnsChatAsync_ShouldReturnFalse_WhenChatHasNoTransportRequest()
        {
            var chat = new Chats { ChatId = 1, TransportRequest = null };
            _repoMock.Setup(r => r.GetChatWithRelationsAsync(1)).ReturnsAsync(chat);

            var result = await _service.UserOwnsChatAsync(10, 1);

            Assert.False(result);
        }
    }
}
