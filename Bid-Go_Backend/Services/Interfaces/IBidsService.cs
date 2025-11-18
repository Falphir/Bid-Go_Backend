using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.DTOs;
using Bid_Go_Backend.Data.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bid_Go_Backend.Services.Interfaces
{
    /// <summary>
    /// Service responsible for bid lifecycle operations: create, update, cancel and queries.
    /// Business rules (e.g., bid validity relative to transport request) are enforced in the service.
    /// </summary>
    public interface IBidsService
    {
        /// <summary>
        /// Create a new bid for a transport request on behalf of a driver.
        /// </summary>
        /// <param name="id">Driver identifier.</param>
        /// <param name="bidDto">Bid creation data.</param>
        /// <returns>Tuple indicating success, a message and the created Bid when successful.</returns>
        Task<(bool Success, string Message, Bid? Bid)> AddBidAsync(int id,AddBidDTO bidDto);

        /// <summary>
        /// Update fields of an existing bid when allowed by status.
        /// </summary>
        /// <param name="id">Bid identifier.</param>
        /// <param name="updateDto">Fields to update.</param>
        /// <returns>Tuple indicating success, a message and the updated Bid when successful.</returns>
        Task<(bool Success, string Message, Bid? Bid)> UpdateBidAsync(int id, BidUpdateDTO updateDto);

        /// <summary>
        /// Cancel a pending bid.
        /// </summary>
        /// <param name="id">Bid identifier.</param>
        /// <returns>Tuple indicating success and a message.</returns>
        Task<(bool Success, string Message)> CancelBidAsync(int id);

        /// <summary>
        /// Retrieve a bid by identifier.
        /// </summary>
        /// <param name="id">Bid identifier.</param>
        /// <returns>Bid when found otherwise null.</returns>
        Task<Bid?> GetBidByIdAsync(int id);

        /// <summary>
        /// Get all bids associated with a transport request.
        /// </summary>
        /// <param name="transportRequestId">Transport request identifier.</param>
        /// <returns>List of bids for the transport request.</returns>
        Task<List<Bid>> GetBidsByTransportRequestAsync(int transportRequestId);

        /// <summary>
        /// Get bids for a transport request filtered by status.
        /// </summary>
        /// <param name="transportRequestId">Transport request identifier.</param>
        /// <param name="status">Bid status filter.</param>
        /// <returns>Enumerable of bids matching the filter.</returns>
        Task<IEnumerable<Bid>> GetBidsByTransportRequestAndStatusAsync(int transportRequestId, EBidStatus status);

        /// <summary>
        /// Get active bids for a transport request with optional ordering.
        /// </summary>
        /// <param name="transportRequestId">Transport request identifier.</param>
        /// <param name="orderBy">Field to order by (default: value).</param>
        /// <param name="descending">Whether to sort descending.</param>
        /// <returns>List of active bids.</returns>
        Task<List<Bid>> GetActiveBidsAsync(int transportRequestId, string? orderBy = "value", bool descending = false);


        Task<List<Bid>> GetBidsByDriverId(int driverId);
    }
}