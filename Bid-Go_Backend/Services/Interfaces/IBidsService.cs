using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.DTOs;
using Bid_Go_Backend.Data.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bid_Go_Backend.Services.Interfaces
{
    public interface IBidsService
    {
        Task<(bool Success, string Message, Bid? Bid)> AddBidAsync(AddBidDTO bidDto);
        Task<(bool Success, string Message, Bid? Bid)> UpdateBidAsync(int id, BidUpdateDTO updateDto);
        Task<(bool Success, string Message)> CancelBidAsync(int id);
        Task<Bid?> GetBidByIdAsync(int id);
        Task<List<Bid>> GetBidsByTransportRequestAsync(int transportRequestId);
        Task<IEnumerable<Bid>> GetBidsByTransportRequestAndStatusAsync(int transportRequestId, EBidStatus status);
        Task<List<Bid>> GetActiveBidsAsync(int transportRequestId, string? orderBy = "value", bool descending = false);
    }
}