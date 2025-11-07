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
    /// <summary>
    /// Repository for storing and querying reviews for drivers and companies.
    /// </summary>
    public class ReviewRequestRepository : IReviewRequestRepository
    {
        private readonly BidGoDbContext _context;
        private readonly ILogger<ReviewRequestRepository> _logger;
        public ReviewRequestRepository(BidGoDbContext context, ILogger<ReviewRequestRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get a transport request by identifier.
        /// </summary>
        public async Task<TransportRequest?> GetTransportRequestAsync(int transportRequestId)
        {
            return await _context.TransportRequests
                .FirstOrDefaultAsync(t => t.TransportRequestId == transportRequestId);
        }

        /// <summary>
        /// Check whether a company review already exists for a given service.
        /// </summary>
        public async Task<bool> CompanyReviewExistsAsync(int transportRequestId, int companyId)
        {
            return await _context.Reviews
                .OfType<ReviewCompany>()
                .AnyAsync(r => r.TransportRequestId == transportRequestId && r.CompanyId == companyId);
        }

        /// <summary>
        /// Check whether a driver review already exists for a given service.
        /// </summary>
        public async Task<bool> DriverReviewExistsAsync(int transportRequestId, int driverId)
        {
            return await _context.Reviews
                .OfType<ReviewDriver>()
                .AnyAsync(r => r.TransportRequestId == transportRequestId && r.DriverId == driverId);
        }

        /// <summary>
        /// Add a review and persist it.
        /// </summary>
        public async Task AddReviewAsync(Data.Models.Review review)
        {
            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Get a unified list of reviews for a service composed of company and driver reviews joined with user names.
        /// </summary>
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
