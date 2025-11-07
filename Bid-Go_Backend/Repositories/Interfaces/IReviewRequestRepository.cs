using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Bid_Go_Backend.Repositories.Interfaces
{
    public interface IReviewRequestRepository
    {
        // Reads used by the service
        Task<TransportRequest?> GetTransportRequestAsync(int transportRequestId);
        Task<bool> CompanyReviewExistsAsync(int transportRequestId, int companyId);
        Task<bool> DriverReviewExistsAsync(int transportRequestId, int driverId);

        // Write used by the service
        Task AddReviewAsync(Bid_Go_Backend.Data.Models.Review review);

        // Read projection for listing reviews by service
        Task<IEnumerable<ReviewByServiceDTO>> GetReviewByServiceIdAsync(int transportRequestId);
    }
}
