using Bid_Go_Backend.Data;
using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.DTOs;
using Bid_Go_Backend.Data.Models.Enums;
using Bid_Go_Backend.Data.Repositories.Interfaces;
using System;


namespace Bid_Go_Backend.Data.Repositories.Transport_Request
{
    public class TransportRequestRepository : ITransportRequestRepository
    {
        private readonly BidGoDbContext _context;


        public TransportRequestRepository(BidGoDbContext context)
        {
            _context = context;
        }

        public async Task<TransportRequest> CreateAsync(CreateTransportRequestDTO dto)
        {
            // Validação: datas coerentes
            if (dto.PickupDate >= dto.DeliveryDate)
                throw new ArgumentException("A data de recolha deve ser anterior à data de entrega.");

            // Validação: imagem obrigatória
            if (string.IsNullOrWhiteSpace(dto.Image))
                throw new ArgumentException("A imagem é obrigatória para publicar o pedido.");

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
                Image = dto.Image,
                CompanyId = dto.CompanyId,
                Status = ERequestStatus.Active 
            };

            _context.TransportRequests.Add(request);
            await _context.SaveChangesAsync();

            return request;
        }

        public async Task<TransportRequest?> UpdateAsync(int id, UpdateTransportRequestDTO dto)
        {
            var request = await _context.TransportRequests.FindAsync(id);
            if (request == null)
                return null;

            if (request.Status != ERequestStatus.Active)
                throw new InvalidOperationException("Só é possível atualizar pedidos ativos antes do leilão.");

            if (dto.PickupDate.HasValue && dto.DeliveryDate.HasValue &&
                dto.PickupDate.Value >= dto.DeliveryDate.Value)
                throw new ArgumentException("A data de recolha deve ser anterior à de entrega.");

            if (!string.IsNullOrEmpty(dto.Image))
                request.Image = dto.Image;

            request.Origin = dto.Origin ?? request.Origin;
            request.Destination = dto.Destination ?? request.Destination;
            request.Weight = dto.Weight ?? request.Weight;
            request.Volume = dto.Volume ?? request.Volume;
            request.PickupDate = dto.PickupDate ?? request.PickupDate;
            request.DeliveryDate = dto.DeliveryDate ?? request.DeliveryDate;

            _context.TransportRequests.Update(request);
            await _context.SaveChangesAsync();

            return request;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var request = await _context.TransportRequests.FindAsync(id);
            if (request == null)
                return false;

            if (request.Status != ERequestStatus.Active)
                throw new InvalidOperationException("Só é possível cancelar pedidos ativos.");

            _context.TransportRequests.Remove(request);
            await _context.SaveChangesAsync();
            return true;
        }


    }
}