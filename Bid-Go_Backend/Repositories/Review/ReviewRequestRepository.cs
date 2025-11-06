using Bid_Go_Backend.Data;
using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.DTOs;
using Bid_Go_Backend.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Bid_Go_Backend.Repositories.Review
{
    public class ReviewRequestRepository : IReviewRequestRepository
    {
        private readonly BidGoDbContext _context;
        private readonly ILogger<ReviewRequestRepository> _logger;
        public ReviewRequestRepository(BidGoDbContext context, ILogger<ReviewRequestRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<TransportRequest?> GetTransportRequestAsync(int transportRequestId)
        {
            return await _context.TransportRequests
                .FirstOrDefaultAsync(t => t.TransportRequestId == transportRequestId);
        }

        public async Task<bool> CompanyReviewExistsAsync(int transportRequestId, int companyId)
        {
            return await _context.Reviews
                .OfType<ReviewCompany>()
                .AnyAsync(r => r.TransportRequestId == transportRequestId && r.CompanyId == companyId);
        }

        public async Task<bool> DriverReviewExistsAsync(int transportRequestId, int driverId)
        {
            return await _context.Reviews
                .OfType<ReviewDriver>()
                .AnyAsync(r => r.TransportRequestId == transportRequestId && r.DriverId == driverId);
        }

        public async Task AddReviewAsync(Data.Models.Review review)
        {
            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<ReviewByServiceDTO>> GetReviewByServiceIdAsync(int transportRequestId)
        {
            var transport = await _context.TransportRequests
                .FirstOrDefaultAsync(t => t.TransportRequestId == transportRequestId);

            if (transport == null)
                throw new InvalidOperationException("Serviço não encontrado.");

            var companyReviews = await _context.Reviews
                .OfType<ReviewCompany>()
                .Where(r => r.TransportRequestId == transportRequestId)
                .Join(
                    _context.Users,
                    review => review.DriverId,
                    user => user.Id,
                    (review, user) => new ReviewByServiceDTO
                    {
                        TimeStamp = review.TimeStamp,
                        Classification = review.Classification,
                        Name = user.Name,
                        ServiceQuality = review.ServiceQuality,
                        ClientSuport = review.ClientSuport,
                        Punctuality = null,
                        Behavior = null
                    }
                ).ToListAsync();

            var driverReviews = await _context.Reviews
                .OfType<ReviewDriver>()
                .Where(r => r.TransportRequestId == transportRequestId)
                .Join(
                    _context.Users,
                    review => review.CompanyId,
                    user => user.Id,
                    (review, user) => new ReviewByServiceDTO
                    {
                        TimeStamp = review.TimeStamp,
                        Classification = review.Classification,
                        Name = user.Name,
                        Punctuality = review.Punctuality,
                        Behavior = review.Behavior,
                        ServiceQuality = null,
                        ClientSuport = null
                    }
                ).ToListAsync();

            return companyReviews.Concat(driverReviews);
        }
    }
}
