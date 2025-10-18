using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.DTOs;
using Bid_Go_Backend.Data.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bid_Go_Backend.Data.Repositories.Requests
{
    public class HistoryRepository : IHistoryRepository
    {
        private readonly BidGoDbContext _context;
        public HistoryRepository(BidGoDbContext context)
        {
            _context = context;
        }

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
                    Rating = review != null ? review.Classification : (int?)null
                }
            ).ToListAsync();

            return bidHistory;
        }


        //public async Task<List<TransportHistoryDTO>> GetTransportHistoryAsync(int companyId)
        //{
        //}
    }
}
