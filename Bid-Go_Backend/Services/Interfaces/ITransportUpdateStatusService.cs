using Bid_Go_Backend.Data.Models.DTOs;
using Bid_Go_Backend.Data.Models.Enums;
using System.Threading.Tasks;

namespace Bid_Go_Backend.Services.Interfaces
{
    public interface ITransportUpdateStatusService
    {
        Task<TransportRequestResponseDTO?> UpdateRequestStatusAsync(int id, int companyID, ERequestStatus newStatus);
    }
}
