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
            var transportRequest = await _ctx.TransportRequests
                .Include(tr => tr.Bids)
                .ThenInclude(b => b.Driver)
                .FirstOrDefaultAsync(tr => tr.TransportRequestId == transportRequestId);

            if (transportRequest == null)
                return new AutomaticSelectionResult { Message = "Transport request not found." };

            // Validações
            if (!transportRequest.IsAutomaticSelectionEnabled)
                return new AutomaticSelectionResult { Message = "Automatic Selection is not enabled." };

            if (transportRequest.BiddingEndDate > DateTime.UtcNow)
                return new AutomaticSelectionResult { Message = "Bidding has not finished yet." };

            if (transportRequest.Status == ERequestStatus.Canceled)
                return new AutomaticSelectionResult { Message = "The transport request is canceled." };

            if (!transportRequest.Bids.Any())
                return new AutomaticSelectionResult { Message = "No bids were submitted for this transport request." };

            // Calcular reputações
            var driverIds = transportRequest.Bids.Select(b => b.DriverId).Distinct().ToList();

            var reputations = await _ctx.Reviews
                .Where(r => driverIds.Contains(r.DriverId))
                .GroupBy(r => r.DriverId)
                .Select(g => new
                {
                    DriverId = g.Key,
                    AverageClassification = g.Average(r => r.Classification)
                })
                .ToDictionaryAsync(r => r.DriverId, r => r.AverageClassification);

            // Filtrar bids elegíveis (reputação >= 3)
            var eligibleBids = transportRequest.Bids
                .Where(b => reputations.GetValueOrDefault(b.DriverId, 0) >= 3)
                .ToList();

            if (!eligibleBids.Any())
                return new AutomaticSelectionResult { Message = "No eligible bids found for this transport request." };

            // Selecionar Preço
            var minPrice = eligibleBids.Min(b => b.Value);
            var lowestBids = eligibleBids.Where(b => b.Value == minPrice).ToList();

            // Desempate Reputação
            var maxReputation = lowestBids.Max(b => reputations.GetValueOrDefault(b.DriverId, 0));
            var bestBids = lowestBids
                .Where(b => reputations.GetValueOrDefault(b.DriverId, 0) == maxReputation)
                .ToList();

            // Desempate ordem de submissão
            var selectedBid = bestBids.Count == 1
                ? bestBids.First()
                : bestBids.OrderBy(b => b.BidId).First();

            // Guardar Bid Selecionada na BD
            transportRequest.SelectedBidId = selectedBid.BidId;
            await _ctx.SaveChangesAsync();

            return new AutomaticSelectionResult { SelectedBid = selectedBid };
        }
    }
}
