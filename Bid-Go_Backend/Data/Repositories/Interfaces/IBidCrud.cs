using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bid_Go_Backend.Data.Models;

namespace Bid_Go_Backend.Repositories.Interface
{
    public interface IBidCRUD
    {
        Task<Bid> CreateBidAsync(Bid bid);
        Task<Bid?> UpdateBidAsync(int id, Bid bid);
        Task<Bid?> GetBidByIdAsync(int id);
        Task<bool> CancelBidAsync(int id);

    }
}