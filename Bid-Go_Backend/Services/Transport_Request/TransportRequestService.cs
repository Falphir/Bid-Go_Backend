using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.DTOs;
using Bid_Go_Backend.Data.Models.Enums;
using Bid_Go_Backend.Repositories.Interfaces;
using Bid_Go_Backend.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Bid_Go_Backend.Services.Transport_Request
{
    /// <summary>
    /// Service for CRUD and validation logic around transport requests.
    /// </summary>
    public class TransportRequestService : ITransportRequestService
    {
        private readonly ITransportRequestRepository _repository;
        private readonly ICloudflareR2Service _r2Service;
        public TransportRequestService(ITransportRequestRepository repository, ICloudflareR2Service r2Service)
        {
            _repository = repository;  
            _r2Service = r2Service;
            
        }

        /// <summary>
        /// Create a new transport request with image upload and validation rules.
        /// </summary>
        /// <param name="dto">Creation DTO.</param>
        /// <param name="imageFile">Image file for the request.</param>
        public async Task<TransportRequest> CreateAsync(CreateTransportRequestDTO dto, IFormFile imageFile)
        {

            if (imageFile == null || imageFile.Length == 0)
                throw new ArgumentException("A imagem é obrigatória.");

            // Salva a imagem localmente e obtém o caminho relativo
            string imageUrl = await _r2Service.UploadImageAsync(imageFile);

            if (dto.PickupDate >= dto.DeliveryDate)
                throw new ArgumentException("A data de recolha deve ser anterior à data de entrega.");
            if (dto.BiddingStartDate >= dto.BiddingEndDate)
                throw new ArgumentException("A data de início das licitações deve ser anterior à data de fim.");
            if (dto.PickupDate <= dto.BiddingEndDate)
                throw new ArgumentException("A data de recolha deve ser posterior ao fim das licitações.");
            if (dto.Weight <= 0 || dto.Volume <= 0)
                throw new ArgumentException("Peso e volume devem ser superiores a zero.");
            if (dto.MaxPrice < 20)
                throw new ArgumentException("O preço deve ser igual ou superior a 20.");
            if (dto.Length <= 0 || dto.Width <= 0 || dto.Height <= 0)
                throw new ArgumentException("As dimensões devem ser superiores a zero.");

            var request = new TransportRequest
            {
                Origin = dto.Origin,
                Destination = dto.Destination,
                Package = dto.Package,
                Weight = dto.Weight,
                Volume = dto.Volume,
                Length = dto.Length,
                Width = dto.Width,
                Height = dto.Height,
                PickupDate = dto.PickupDate,
                DeliveryDate = dto.DeliveryDate,
                BiddingStartDate = dto.BiddingStartDate,
                BiddingEndDate = dto.BiddingEndDate,
                IsAutomaticSelectionEnabled = dto.IsAutomaticSelectionEnabled,
                Image = imageUrl,
                MaxPrice = dto.MaxPrice,
                CompanyId = dto.CompanyId,
                Status = ERequestStatus.Active
            };

            return await _repository.CreateAsync(request);
        }

        /// <summary>
        /// Update a draft transport request applying provided fields and optionally replacing the image.
        /// </summary>
        public async Task<TransportRequest?> UpdateAsync(int id, UpdateTransportRequestDTO dto, IFormFile? imageFile)

        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null)
                throw new KeyNotFoundException("Pedido não encontrado.");

            if (existing.Status != ERequestStatus.Draft)
                throw new InvalidOperationException("Apenas pedidos com estado DRAFT podem ser atualizados.");

            if (dto.PickupDate.HasValue && dto.DeliveryDate.HasValue && dto.PickupDate >= dto.DeliveryDate)
                throw new ArgumentException("A data de recolha deve ser anterior à data de entrega.");

            if (dto.BiddingStartDate.HasValue && dto.BiddingEndDate.HasValue && dto.BiddingStartDate >= dto.BiddingEndDate)
                throw new ArgumentException("A data de início das licitações deve ser anterior à data de fim.");

            if (dto.PickupDate <= dto.BiddingEndDate)
                throw new ArgumentException("A data de recolha deve ser posterior ao fim das licitações.");

            if (dto.MaxPrice.HasValue && dto.MaxPrice < 20)
                throw new ArgumentException("O preço deve ser igual ou superior a 20.");

            if (dto.Length.HasValue && dto.Length <= 0 ||
                dto.Width.HasValue && dto.Width <= 0 ||
                dto.Height.HasValue && dto.Height <= 0)
                throw new ArgumentException("As dimensões devem ser superiores a zero.");

            if (dto.Volume.HasValue && dto.Volume <= 0)
                throw new ArgumentException("O volume deve ser superior a zero.");

            existing.Origin = dto.Origin ?? existing.Origin;
            existing.Destination = dto.Destination ?? existing.Destination;
            existing.Package = dto.Package ?? existing.Package;
            existing.Weight = dto.Weight ?? existing.Weight;
            existing.Volume = dto.Volume ?? existing.Volume;
            existing.Length = dto.Length ?? existing.Length;
            existing.Width = dto.Width ?? existing.Width;
            existing.Height = dto.Height ?? existing.Height;
            existing.PickupDate = dto.PickupDate ?? existing.PickupDate;
            existing.DeliveryDate = dto.DeliveryDate ?? existing.DeliveryDate;
            existing.BiddingStartDate = dto.BiddingStartDate ?? existing.BiddingStartDate;
            existing.BiddingEndDate = dto.BiddingEndDate ?? existing.BiddingEndDate;
            existing.IsAutomaticSelectionEnabled = dto.IsAutomaticSelectionEnabled ?? existing.IsAutomaticSelectionEnabled;
            existing.MaxPrice = dto.MaxPrice ?? existing.MaxPrice;

            if (imageFile != null && imageFile.Length > 0)
            {
                
                if (!string.IsNullOrEmpty(existing.Image))
                {
                    await _r2Service.DeleteImageAsync(existing.Image);
                }

                // faz upload da nova imagem
                var imageUrl = await _r2Service.UploadImageAsync(imageFile);
                existing.Image = imageUrl;
            }


            return await _repository.UpdateAsync(id, existing);
        }

        /// <summary>
        /// Delete an active transport request.
        /// </summary>
        public async Task<bool> DeleteAsync(int id)
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null)
                throw new KeyNotFoundException("Pedido não encontrado.");

            if (existing.Status != ERequestStatus.Active)
                throw new InvalidOperationException("Apenas pedidos ativos podem ser eliminados.");

            return await _repository.DeleteAsync(id);
        }

        /// <summary>
        /// Get a transport request by identifier.
        /// </summary>
        public async Task<TransportRequest?> GetByIdAsync(int id)
        {
            return await _repository.GetByIdAsync(id);
        }

        /// <summary>
        /// List transport requests for a company.
        /// </summary>
        public async Task<List<TransportRequest>> GetByCompanyAsync(int companyId)
        {
            return await _repository.GetAllByCompanyAsync(companyId);
        }

    }
}
