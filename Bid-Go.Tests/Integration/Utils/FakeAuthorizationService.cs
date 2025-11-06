using Bid_Go_Backend.Services.Interfaces;
using Bid_Go_Backend.Data.Models.Enums;

namespace Bid_Go.Tests.Integration.Utils
{
 public class FakeAuthorizationService : IAuthorizationService
 {
 public Task<bool> CompanyOwnsTransportRequestAsync(int companyId, int transportRequestId)
 {
 return Task.FromResult(true); // configurable later if needed
 }
 public Task<bool> DriverOwnsBidAsync(int driverId, int bidId) => Task.FromResult(true);
 public Task<bool> CompanyOwnsPaymentAsync(int companyId, int paymentId) => Task.FromResult(true);
 public Task<bool> UserOwnsChatAsync(int userId, int chatId) => Task.FromResult(true);
 }
}
