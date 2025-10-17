using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.DTOs;
using Bid_Go_Backend.Data.Models.Enums;
using Bid_Go_Backend.Data.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bid_Go_Backend.Data.Repositories.Requests
{
    public class TransportRequestRepository : ITransportRequestRepository
    {
        private readonly BidGoDbContext _context;
        public TransportRequestRepository(BidGoDbContext context)
        {
            _context = context;
        }

        public async Task<TransportRequest> UpdateRequestStatusAsync(int id, int companyID, ERequestStatus status)
        {
            var request = await _context.TransportRequests.FindAsync(id);
            if (request == null)
            {
                throw new Exception("Transport request not found");
            }

            if (request.CompanyId != companyID && (request.Status != ERequestStatus.Active && request.Status != ERequestStatus.Instransit))
            {
                throw new InvalidOperationException("Não tem permissão para atualizar o estado deste pedido.");
            }

            if (status == ERequestStatus.Canceled && (request.Status != ERequestStatus.Active && request.Status != ERequestStatus.Instransit))
            {
                throw new InvalidOperationException("Só é possível cancelar pedidos ativos ou em trânsito.");
            }

            request.Status = status;
            _context.TransportRequests.Update(request);
            await _context.SaveChangesAsync();
            return request;
        }
    }
}
