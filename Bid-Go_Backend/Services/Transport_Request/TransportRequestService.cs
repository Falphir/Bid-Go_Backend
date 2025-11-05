using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.DTOs;
using Bid_Go_Backend.Data.Models.Enums;
using Bid_Go_Backend.Data.Repositories.Interfaces;
using Bid_Go_Backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Bid_Go_Backend.Services.Transport_Request
{
    public class TransportRequestService : ITransportRequestService
    {
        private readonly ITransportRequestRepository _repository;

        public TransportRequestService(ITransportRequestRepository repository)
        {
            _repository = repository;
        }

        public async Task<TransportRequest> CreateAsync(CreateTransportRequestDTO dto)
        {
       
            if (dto.PickupDate >= dto.DeliveryDate)
                throw new ArgumentException("A data de recolha deve ser anterior à data de entrega.");
            if (dto.BiddingStartDate >= dto.BiddingEndDate)
                throw new ArgumentException("A data de início das licitações deve ser anterior à data de fim.");
            if (dto.PickupDate <= dto.BiddingEndDate)
                throw new ArgumentException("A data de recolha deve ser posterior ao fim das licitações.");
            if (string.IsNullOrWhiteSpace(dto.Image))
                throw new ArgumentException("A imagem é obrigatória.");
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
                Image = dto.Image,
                MaxPrice = dto.MaxPrice,
                CompanyId = dto.CompanyId,
                Status = ERequestStatus.Draft
            };

            return await _repository.CreateAsync(request);
        }

        public async Task<TransportRequest?> UpdateAsync(int id, UpdateTransportRequestDTO dto)
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
            existing.Image = dto.Image ?? existing.Image;
            existing.PickupDate = dto.PickupDate ?? existing.PickupDate;
            existing.DeliveryDate = dto.DeliveryDate ?? existing.DeliveryDate;
            existing.BiddingStartDate = dto.BiddingStartDate ?? existing.BiddingStartDate;
            existing.BiddingEndDate = dto.BiddingEndDate ?? existing.BiddingEndDate;
            existing.IsAutomaticSelectionEnabled = dto.IsAutomaticSelectionEnabled ?? existing.IsAutomaticSelectionEnabled;
            existing.MaxPrice = dto.MaxPrice ?? existing.MaxPrice;

            return await _repository.UpdateAsync(id, existing);
        }


        public async Task<bool> DeleteAsync(int id)
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null)
                throw new KeyNotFoundException("Pedido não encontrado.");

            if (existing.Status != ERequestStatus.Active)
                throw new InvalidOperationException("Apenas pedidos ativos podem ser eliminados.");

            return await _repository.DeleteAsync(id);
        }

        public async Task<TransportRequest?> GetByIdAsync(int id)
        {
            return await _repository.GetByIdAsync(id);
        }

        public async Task<List<TransportRequest>> GetByCompanyAsync(int companyId)
        {
            return await _repository.GetAllByCompanyAsync(companyId);
        }



    }
}
