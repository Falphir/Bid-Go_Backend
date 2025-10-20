using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bid_Go_Backend.Data.Repositories.Login
{
    public class UserRepository : IUserRepository
    {

        private readonly BidGoDbContext _ctx;

        public UserRepository(BidGoDbContext ctx)
        {
            _ctx = ctx;
        }


        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _ctx.Users.FirstOrDefaultAsync(u => u.Email == email);
        }
    }
}
