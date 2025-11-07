using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.DTOs;
using Bid_Go_Backend.Data.Models.Enums;
using Bid_Go_Backend.Repositories.Interfaces;
using Bid_Go_Backend.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Bid_Go_Backend.Services.Review
{
    /// <summary>
    /// Service that validates and stores reviews for drivers and companies.
    /// </summary>
    public class ReviewRequestService : IReviewRequestService
    {
        private readonly IReviewRequestRepository _repository;
        private readonly ILogger<ReviewRequestService> _logger;

        public ReviewRequestService(IReviewRequestRepository repository, ILogger<ReviewRequestService> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        /// <summary>
        /// Submit a review for a completed transport service.
        /// </summary>
        /// <param name="reviewDto">DTO carrying review payload and discriminator.</param>
        public async Task<bool> SubmitReviewAsync(ReviewRequestServiceDTO reviewDto)
        {
            _logger.LogDebug("Submitting review for transport request {RequestId}", reviewDto.TransportRequestId);

            var transport = await _repository.GetTransportRequestAsync(reviewDto.TransportRequestId);
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
                "Company" => await _repository.CompanyReviewExistsAsync(reviewDto.TransportRequestId, reviewDto.CompanyId),
                "Driver" => await _repository.DriverReviewExistsAsync(reviewDto.TransportRequestId, reviewDto.DriverId),
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

            await _repository.AddReviewAsync(review);
            return true;
        }

        /// <summary>
        /// Get reviews by transport request identifier.
        /// </summary>
        public async Task<IEnumerable<ReviewByServiceDTO>> GetReviewByServiceIdAsync(int transportRequestId)
        {
            _logger.LogDebug("Getting reviews for transport request {RequestId}", transportRequestId);
            return await _repository.GetReviewByServiceIdAsync(transportRequestId);
        }
    }
}
