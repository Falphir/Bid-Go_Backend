using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.Enums;
using Bid_Go_Backend.Data.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bid_Go_Backend.Data.Repositories.Bids
{
    internal class AutomaticSelectionAlgorithmRepository : IAutomaticSelectionAlgorithmRepository
    {
        private readonly BidGoDbContext _ctx;

        public AutomaticSelectionAlgorithmRepository(BidGoDbContext ctx)
        {
            _ctx = ctx;
        }

        public async Task<IEnumerable<Bid>> GetEligibleBidsAsync(int transportRequestId)
        {
            var bids = await _ctx.Bids
                 .Include(b => b.Driver)
                 .Where(b => b.TransportRequestId == transportRequestId)
                 .ToListAsync();

            var driverIds = bids
                .Select(b => b.DriverId)
                .Distinct()
                .ToList();

            var reputations = await _ctx.Reviews
                .Where(r => driverIds.Contains(r.DriverId))
                .GroupBy(r => r.DriverId)
                .Select(g => new
                {
                    DriverId = g.Key,
                    AverageClassification = g.Average(r => r.Classification)
                })
                .ToListAsync();

            var eligibleDriverIds = reputations
                .Where(r => r.AverageClassification >= 3.0m)
                .Select(r => r.DriverId)
                .ToHashSet();

            return bids.Where(b => eligibleDriverIds.Contains(b.DriverId));
        }

        public async Task<bool> IsTransportRequestCanceledAsync(int transportRequestId)
        {
            return await _ctx.TransportRequests
                .Where(tr => tr.TransportRequestId == transportRequestId)
                .Select(tr => tr.Status == ERequestStatus.Canceled)
                .FirstOrDefaultAsync();
        }

        public async Task<AutomaticSelectionResult> ExecuteAutomaticSelectionAsync(int transportRequestId)
        {
            if (await IsTransportRequestCanceledAsync(transportRequestId))
                return new AutomaticSelectionResult
                {
                    Message = "The transport request is canceled."
                };

            var allBids = await _ctx.Bids
                .Include(b => b.Driver)
                .Where(b => b.TransportRequestId == transportRequestId)
                .ToListAsync();

            if (!allBids.Any())
                return new AutomaticSelectionResult
                {
                    Message = "No bids were submitted for this transport request."
                };

            var eligibleBids = await GetEligibleBidsAsync(transportRequestId);

            if (!eligibleBids.Any())
                return new AutomaticSelectionResult
                {
                    Message = "No eligible bids found for this transport request."
                };

            var minPrice = eligibleBids.Min(b => b.Value);
            var lowestBids = eligibleBids.Where(b => b.Value == minPrice).ToList();

            if (lowestBids.Count == 1)
                return new AutomaticSelectionResult { SelectedBid = lowestBids.First() };

            var driverIds = lowestBids.Select(b => b.DriverId).Distinct().ToList();
            var reputations = await _ctx.Reviews
                .Where(r => driverIds.Contains(r.DriverId))
                .GroupBy(r => r.DriverId)
                .Select(g => new
                {
                    DriverId = g.Key,
                    AverageClassification = g.Average(r => r.Classification)
                })
                .ToListAsync();

            var bidsWithReputation = lowestBids
                .Select(b => new
                {
                    Bid = b,
                    Reputation = reputations.FirstOrDefault(r => r.DriverId == b.DriverId)?.AverageClassification ?? 0
                })
                .ToList();

            var maxReputation = bidsWithReputation.Max(b => b.Reputation);
            var bestReputationBids = bidsWithReputation
                .Where(b => b.Reputation == maxReputation)
                .Select(b => b.Bid)
                .ToList();

            var selectedBid = bestReputationBids.Count == 1
                ? bestReputationBids.First()
                : bestReputationBids.OrderBy(b => b.BidId).First();

            return new AutomaticSelectionResult { SelectedBid = selectedBid };
        }

    }
}
