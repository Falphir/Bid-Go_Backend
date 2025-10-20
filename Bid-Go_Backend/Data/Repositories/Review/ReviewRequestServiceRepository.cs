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
            // 1️⃣ Validar se o serviço existe e está concluído
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

            // 2️⃣ Verificar se já existe avaliação para aquele serviço e partes
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


            // 3️⃣ Validar nota
            if (reviewDto.Classification < 1 || reviewDto.Classification > 5)
            {
                throw new ArgumentOutOfRangeException(nameof(reviewDto.Classification),
                    "A classificação deve estar entre 1 e 5.");
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
    }
}
