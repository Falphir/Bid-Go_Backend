using Bid_Go_Backend.Data.Models.DTOs;
using Bid_Go_Backend.Data.Models.Enums;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Bid_Go_Backend.Services.Interfaces
{
    public interface ITransportUpdateStatusService
    {
        Task<(int StatusCode, object Body)> UpdateRequestStatusAsync(
           int requestId,
           ClaimsPrincipal user,
           ERequestStatus newStatus);

    }
}
