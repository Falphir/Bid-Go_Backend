using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.DTOs;
using Bid_Go_Backend.Data.Models.Enums;
using Bid_Go_Backend.Data.Repositories.Interfaces;

namespace Bid_Go_Backend.Data.Repositories.Transport_Request
{
    public class TransportUpdateStatusRepository : ITransportUpdateStatus
    {
        private readonly BidGoDbContext _context;
        public TransportUpdateStatusRepository(BidGoDbContext context)
        {
            _context = context;
        }

        public async Task<TransportRequest> UpdateRequestStatusAsync(int id, int companyID, ERequestStatus newStatus)
        {
            var request = await _context.TransportRequests.FindAsync(id);
            if (request == null)
            {
                throw new Exception("Transport request not found.");
            }

            var user = await _context.Users.FindAsync(companyID);
            if (user == null)
            {
                throw new InvalidOperationException("Utilizador não encontrado.");
            }

            bool isValidTransition = false;
            string userRole;

            if (user is Company)
            {
                userRole = "Company";
                isValidTransition = request.Status switch
                {
                    ERequestStatus.Active => newStatus is ERequestStatus.Pending or ERequestStatus.Canceled,
                    ERequestStatus.Pending => newStatus == ERequestStatus.WaitingPickup,
                    _ => false
                };
            }
            else if (user is Driver)
            {
                userRole = "Driver";
                isValidTransition = request.Status switch
                {
                    ERequestStatus.WaitingPickup => newStatus == ERequestStatus.InTransit,
                    ERequestStatus.InTransit => newStatus is ERequestStatus.Completed or ERequestStatus.Canceled,
                    _ => false
                };
            }
            else
            {
                throw new InvalidOperationException("Tipo de utilizador não suportado.");
            }

            if (!isValidTransition)
            {
                throw new InvalidOperationException(
                    $"Não é possível mudar o estado de '{request.Status}' para '{newStatus}' para o tipo '{userRole}'.");
            }

            // Atualiza o estado
            request.Status = newStatus;
            _context.TransportRequests.Update(request);
            await _context.SaveChangesAsync();

            return request;
        }

    }
}
