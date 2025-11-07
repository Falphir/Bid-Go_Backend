using Bid_Go_Backend.Data;
using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Bid_Go_Backend.Repositories.Login
{
    /// <summary>
    /// Repository for user lookups and updates.
    /// </summary>
    public class UserRepository : IUserRepository
    {
        private readonly BidGoDbContext _ctx;

        public UserRepository(BidGoDbContext ctx)
        {
            _ctx = ctx;
        }

        /// <summary>
        /// Find a user by email.
        /// </summary>
        public async Task<User?> GetByEmailAsync(string email) =>
            await _ctx.Users.FirstOrDefaultAsync(u => u.Email == email);

        /// <summary>
        /// Update a user entity and persist changes. Returns null when user does not exist.
        /// </summary>
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
