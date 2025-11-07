using Bid_Go_Backend.Repositories.Interfaces;
using IAuthorizationService = Bid_Go_Backend.Services.Interfaces.IAuthorizationService;

namespace Bid_Go_Backend.Services
{
    /// <summary>
    /// Service performing authorization checks based on ownership and relations between entities.
    /// </summary>
    public class AuthorizationService : IAuthorizationService
    {
        private readonly IAuthorizationRepository _repo;

        public AuthorizationService(IAuthorizationRepository repo)
        {
            _repo = repo;
        }

        /// <summary>
        /// Check whether a company owns a given transport request.
        /// </summary>
        public async Task<bool> CompanyOwnsTransportRequestAsync(int companyId, int transportRequestId)
        {
            var request = await _repo.GetTransportRequestAsync(transportRequestId);
            Console.WriteLine("Request company ID" + request.CompanyId);
            Console.WriteLine("Company ID" + companyId);
            return request != null && request.CompanyId == companyId;
            
        }

        /// <summary>
        /// Check whether a driver owns a given bid.
        /// </summary>
        public async Task<bool> DriverOwnsBidAsync(int driverId, int bidId)
        {
            var bid = await _repo.GetBidAsync(bidId);
            return bid != null && bid.DriverId == driverId;
        }

        /// <summary>
        /// Check whether a company owns a payment via its transport request.
        /// </summary>
        public async Task<bool> CompanyOwnsPaymentAsync(int companyId, int paymentId)
        {
            var payment = await _repo.GetPaymentAsync(paymentId);
            return payment != null && payment.TransportRequest.CompanyId == companyId;
        }

        /// <summary>
        /// Check if a user (company or driver) has access to a chat based on the selected bid.
        /// </summary>
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

        /// <summary>
        /// Check whether a driver is the selected driver for a transport request.
        /// </summary>
        public async Task<bool> DriverRelatedToTransportRequestAsync(int driverId, int transportRequestId)
        {
            var request = await _repo.GetTransportRequestWithSelectedBidAsync(transportRequestId);

            if (request == null || request.SelectedBid == null)
                return false;

            return request.SelectedBid.DriverId == driverId;
        }
    }
}