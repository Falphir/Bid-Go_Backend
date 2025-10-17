using Bid_Go_Backend.Data;
using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Bid_Go_Backend.Data.Repositories.Transport_Request
{
    public class TransportRequestRepository : ITransportRequestRepository
    {
        private readonly BidGoDbContext _context;

        public TransportRequestRepository(BidGoDbContext context)
        {
            _context = context;
        }

        public async Task<TransportRequest> CreateAsync(TransportRequest request)
        {
            request.Status = Models.Enums.ERequestStatus.Active;
            _context.TransportRequests.Add(request);
            await _context.SaveChangesAsync();
            return request;
        }

        public async Task<TransportRequest?> GetByIdAsync(int id)
        {
            return await _context.TransportRequests.FindAsync(id);
        }

        public async Task<TransportRequest> UpdateAsync(int id,TransportRequest request)
        {

 
            _context.TransportRequests.Update(request);
            await _context.SaveChangesAsync();
            return request;
        }


        public async Task<bool> DeleteAsync(int id)
        {
            var alvo = await _context.TransportRequests.FindAsync(id);

            if(alvo == null)
            {
                return false;
            }


            _context.TransportRequests.Remove(alvo);
            await _context.SaveChangesAsync();
            return true;
        }

    }
}
