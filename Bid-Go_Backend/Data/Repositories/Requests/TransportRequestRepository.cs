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

        public async Task<TransportRequest> UpdateRequestStatusAsync(int id, ERequestStatus status)
        {
            var request = await _context.TransportRequests.FindAsync(id);
            if (request == null)
            {
                throw new Exception("Transport request not found");
            }
            request.Status = status;
            _context.TransportRequests.Update(request);
            await _context.SaveChangesAsync();
            return request;
        }
    }
}
