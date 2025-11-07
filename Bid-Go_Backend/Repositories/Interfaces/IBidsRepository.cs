using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.Enums;

namespace Bid_Go_Backend.Repositories.Interfaces
{
    /// <summary>
    /// Data access contract for Bid entities. Implementations handle persistence and retrieval concerns.
    /// Repositories should not contain business rules; those belong to the service layer.
    /// </summary>
    public interface IBidsRepository
    {
        /// <summary>
        /// Get a bid by its identifier.
        /// </summary>
        Task<Bid?> GetByIdAsync(int id);

        /// <summary>
        /// Persist a new bid to the datastore and return the created entity (with keys populated).
        /// </summary>
        Task<Bid> CreateAsync(Bid bid);

        /// <summary>
        /// Update an existing bid in the datastore and return the updated entity.
        /// </summary>
        Task<Bid> UpdateAsync(Bid bid);

        /// <summary>
        /// Retrieve all bids for a given transport request.
        /// </summary>
        Task<List<Bid>> GetByTransportRequestAsync(int transportRequestId);

        /// <summary>
        /// Retrieve bids for a transport request filtered by status.
        /// </summary>
        Task<IEnumerable<Bid>> GetByTransportRequestAndStatusAsync(int transportRequestId, EBidStatus status);

        /// <summary>
        /// Retrieve active bids for a transport request with optional ordering.
        /// </summary>
        Task<List<Bid>> GetActiveBidsAsync(int transportRequestId, string? orderBy = "value", bool descending = false);
    }

}