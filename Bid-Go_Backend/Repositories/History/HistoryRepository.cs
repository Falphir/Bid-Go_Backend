using Bid_Go_Backend.Data;
using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.DTOs;
using Bid_Go_Backend.Data.Models.Enums;
using Bid_Go_Backend.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Bid_Go_Backend.Repositories.History
{
    /// <summary>
    /// Repository responsible for assembling history DTOs for drivers and companies.
    /// </summary>
    public class HistoryRepository : IHistoryRepository
    {
        private readonly BidGoDbContext _context;
        public HistoryRepository(BidGoDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Get bidding history entries for a driver.
        /// </summary>
        /// <param name="driverId">Driver identifier.</param>
        /// <remarks>
        /// Date is currently returned as a placeholder (2024-01-01) — consider storing event timestamps to provide accurate history.
        /// </remarks>
        public async Task<List<BidHistoryDTO>> GetDriverHistoryAsync(int driverId)
        {
            var bidHistory = await (
                from bid in _context.Bids
                join transport in _context.TransportRequests
                    on bid.TransportRequestId equals transport.TransportRequestId
                join company in _context.Users.OfType<Company>()
                    on transport.CompanyId equals company.Id
                join review in _context.Reviews
                    on new { transport.TransportRequestId, bid.DriverId }
                    equals new { review.TransportRequestId, review.DriverId }
                    into reviewGroup
                from review in reviewGroup.DefaultIfEmpty()
                where bid.DriverId == driverId
                select new BidHistoryDTO
                {
                    CompanyName = company.CompanyName,
                    Package = transport.Package,
                    Destination = transport.Destination,
                    Value = bid.Value,
                    Status = bid.Status,
                    Date = new DateTime(2024, 1, 1),
                    Rating = review != null ? review.Classification : null

                }
            ).ToListAsync();
            return bidHistory;
        }



        /// <summary>
        /// Get transport history entries for a company.
        /// </summary>
        /// <param name="companyId">Company identifier.</param>
        /// <remarks>
        /// Date is currently a placeholder — replace with actual event timestamps when available.
        /// </remarks>
        public async Task<List<TransportHistoryDTO>> GetTransportHistoryAsync(int companyId)
        {
            var history = await (
                from transport in _context.TransportRequests

                join bid in _context.Bids
                    on transport.TransportRequestId equals bid.TransportRequestId into bidsGroup
                from bid in bidsGroup
                    .Where(b => b.Status == EBidStatus.Accepted)
                    .DefaultIfEmpty()
                join driver in _context.Users.OfType<Driver>()
                    on bid.DriverId equals driver.Id into driverGroup
                from driver in driverGroup.DefaultIfEmpty()

                where transport.CompanyId == companyId

                select new TransportHistoryDTO
                {
                    TransportRequestId = transport.TransportRequestId,
                    Package = transport.Package,
                    Name = driver != null ? driver.Name : "No assigned driver",
                    Date = new DateTime(2024, 1, 1), 
                    Destination = transport.Destination,
                    Price = bid != null ? bid.Value : 0,
                    Status = transport.Status.ToString()
                }
            ).ToListAsync();
            return history;
        }
    }   
}
