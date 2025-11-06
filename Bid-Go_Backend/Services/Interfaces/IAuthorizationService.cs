using Bid_Go_Backend.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bid_Go_Backend.Services.Interfaces
{
    public interface IAuthorizationService
    {
        Task<bool> CompanyOwnsTransportRequestAsync(int companyId, int transportRequestId);
        Task<bool> DriverOwnsBidAsync(int driverId, int bidId);
        Task<bool> CompanyOwnsPaymentAsync(int companyId, int paymentId);
        Task<bool> UserOwnsChatAsync(int userId, int chatId);
        Task<bool> DriverRelatedToTransportRequestAsync(int driverId, int transportRequestId);
    }
}
