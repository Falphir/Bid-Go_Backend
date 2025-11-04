using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Bid_Go_Backend.Data.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly BidGoDbContext _ctx;

        public UserRepository(BidGoDbContext ctx)
        {
            _ctx = ctx;
        }

        public async Task<User?> GetByEmailAsync(string email) =>
            await _ctx.Users.FirstOrDefaultAsync(u => u.Email == email);

        public async Task<User?> UpdateAsync(User user)
        {
            var existingUser = await _ctx.Users.FindAsync(user.Id);
            if (existingUser == null)
                return null;

            _ctx.Entry(existingUser).CurrentValues.SetValues(user);
            await _ctx.SaveChangesAsync();
            return existingUser;
        }
    }
}
