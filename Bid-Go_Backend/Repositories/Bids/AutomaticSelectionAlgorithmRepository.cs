using Bid_Go_Backend.Data;
using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.Enums;
using Bid_Go_Backend.Repositories.Interfaces;
using Bid_Go_Backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bid_Go_Backend.Repositories.Bids
{
    internal class AutomaticSelectionAlgorithmRepository : IAutomaticSelectionAlgorithmRepository
    {
        private readonly BidGoDbContext _ctx;

        public AutomaticSelectionAlgorithmRepository(BidGoDbContext ctx)
        {
            _ctx = ctx;
        }

        public async Task<TransportRequest?> GetTransportRequestWithBidsAsync(int transportRequestId)
        {
            return await _ctx.TransportRequests
                .Include(tr => tr.Bids)
                .ThenInclude(b => b.Driver)
                .FirstOrDefaultAsync(tr => tr.TransportRequestId == transportRequestId);
        }

        public async Task<Dictionary<int, decimal>> GetDriverReputationsAsync(IEnumerable<int> driverIds)
        {
            return await _ctx.Reviews
                .Where(r => driverIds.Contains(r.DriverId))
                .GroupBy(r => r.DriverId)
                .Select(g => new { g.Key, Average = g.Average(r => r.Classification) })
                .ToDictionaryAsync(x => x.Key, x => x.Average);
        }

        public async Task SaveChangesAsync() => await _ctx.SaveChangesAsync();
    }

}
