using Bid_Go_Backend.Repositories.Interfaces;
using IAuthorizationService = Bid_Go_Backend.Services.Interfaces.IAuthorizationService;

namespace Bid_Go_Backend.Services
{
    public class AuthorizationService : IAuthorizationService
    {
        private readonly IAuthorizationRepository _repo;

        public AuthorizationService(IAuthorizationRepository repo)
        {
            _repo = repo;
        }

        public async Task<bool> CompanyOwnsTransportRequestAsync(int companyId, int transportRequestId)
        {
            var request = await _repo.GetTransportRequestAsync(transportRequestId);
            Console.WriteLine("Request company ID" + request.CompanyId);
            Console.WriteLine("Company ID" + companyId);
            return request != null && request.CompanyId == companyId;
            
        }

        public async Task<bool> DriverOwnsBidAsync(int driverId, int bidId)
        {
            var bid = await _repo.GetBidAsync(bidId);
            return bid != null && bid.DriverId == driverId;
        }

        public async Task<bool> CompanyOwnsPaymentAsync(int companyId, int paymentId)
        {
            var payment = await _repo.GetPaymentAsync(paymentId);
            return payment != null && payment.TransportRequest.CompanyId == companyId;
        }

        public async Task<bool> UserOwnsChatAsync(int userId, int chatId)
        {
            var chat = await _repo.GetChatWithRelationsAsync(chatId);

            if (chat?.TransportRequest == null)
                return false;

            var tr = chat.TransportRequest;
            var companyId = tr.CompanyId;
            var driverId = tr.SelectedBid?.DriverId;

            return userId == companyId || userId == driverId;
        }
    }
}