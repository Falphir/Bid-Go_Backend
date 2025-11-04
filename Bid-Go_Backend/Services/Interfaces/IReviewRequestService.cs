using Bid_Go_Backend.Data.Models.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Bid_Go_Backend.Services.Interfaces
{
    public interface IReviewRequestService
    {
        Task<bool> SubmitReviewAsync(ReviewRequestServiceDTO reviewDTO);
        Task<IEnumerable<ReviewByServiceDTO>> GetReviewByServiceIdAsync(int transportRequestId);
    }
}
