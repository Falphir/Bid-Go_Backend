using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.DTOs;
using Bid_Go_Backend.Data.Models.Enums;
using Bid_Go_Backend.Data.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bid_Go_Backend.Data.Repositories.Review
{
    public class ReviewRequestServiceRepository : IReviewRequestServiceRepository
    {
        private readonly BidGoDbContext _context;
        private readonly ILogger<HistoryRepository> _logger;
        public ReviewRequestServiceRepository(BidGoDbContext context, ILogger<HistoryRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<bool> SubmitReviewAsync(ReviewRequestServiceDTO reviewDto)
        {
            var transport = await _context.TransportRequests
                .FirstOrDefaultAsync(t => t.TransportRequestId == reviewDto.TransportRequestId);

            if (transport == null)
            {
                _logger.LogWarning("Serviço {Id} não encontrado.", reviewDto.TransportRequestId);
                throw new InvalidOperationException("Serviço não encontrado.");
            }

            if (transport.Status != ERequestStatus.Completed)
            {
                _logger.LogWarning("Serviço {Id} não está concluído.", transport.TransportRequestId);
                throw new InvalidOperationException("O serviço ainda não está concluído.");
            }

            bool reviewExists = reviewDto.Discriminator switch
            {
                "Company" => await _context.Reviews
                    .OfType<ReviewCompany>()
                    .AnyAsync(r =>
                        r.TransportRequestId == reviewDto.TransportRequestId &&
                        r.CompanyId == reviewDto.CompanyId),  // apenas CompanyId e TransportRequestId

                "Driver" => await _context.Reviews
                    .OfType<ReviewDriver>()
                    .AnyAsync(r =>
                        r.TransportRequestId == reviewDto.TransportRequestId &&
                        r.DriverId == reviewDto.DriverId),   // apenas DriverId e TransportRequestId

                _ => throw new InvalidOperationException("Discriminator inválido.")
            };

            if (reviewExists)
            {
                _logger.LogWarning("Já existe avaliação do tipo {Type} para o serviço {Id}.", reviewDto.Discriminator, reviewDto.TransportRequestId);
                throw new InvalidOperationException($"Já existe uma avaliação do tipo {reviewDto.Discriminator} para este serviço.");
            }


            if (reviewDto.Classification < 0 || reviewDto.Classification > 5)
            {
                throw new ArgumentOutOfRangeException(nameof(reviewDto.Classification),
                    "A classificação deve estar entre 0 e 5.");
            }

            Bid_Go_Backend.Data.Models.Review review;

            if (reviewDto.Discriminator == "Company")
            {
                review = new ReviewCompany
                {
                    TimeStamp = reviewDto.TimeStamp,
                    Classification = reviewDto.Classification,
                    DriverId = reviewDto.DriverId,
                    CompanyId = reviewDto.CompanyId,
                    TransportRequestId = reviewDto.TransportRequestId,
                    ServiceQuality = reviewDto.ServiceQuality,
                    ClientSuport = reviewDto.ClientSuport
                };
            }
            else if (reviewDto.Discriminator == "Driver")
            {
                review = new ReviewDriver
                {
                    TimeStamp = reviewDto.TimeStamp,
                    Classification = reviewDto.Classification,
                    DriverId = reviewDto.DriverId,
                    CompanyId = reviewDto.CompanyId,
                    TransportRequestId = reviewDto.TransportRequestId,
                    Punctuality = reviewDto.Punctuality,
                    Behavior = reviewDto.Behavior
                };
            }
            else
            {
                throw new InvalidOperationException("Discriminator inválido.");
            }
            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            return true;
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
