using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Repositories.Interfaces;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Bid_Go.Tests.Integration.Utils
{
    public class FakeUserRepository : IUserRepository
    {
        private readonly ConcurrentDictionary<string, User> _users = new();

        public FakeUserRepository()
        {
            var passwordHash = BCrypt.Net.BCrypt.HashPassword("123456");
            _users["user@test.com"] = new Driver
            {
                Id = 1,
                Email = "user@test.com",
                Password = passwordHash
            };
        }

        public Task<User?> GetByEmailAsync(string email)
        {
            _users.TryGetValue(email, out var user);
            return Task.FromResult(user);
        }

        public Task<User?> UpdateAsync(User user)
        {
            _users[user.Email] = user;
            return Task.FromResult(user);
        }

        // 👇 Método auxiliar extra para os testes (não faz parte da interface)
        public Task<User> CreateUserAsync(User user)
        {
            if (!_users.ContainsKey(user.Email))
            {
                user.Id = _users.Count + 1;
                _users[user.Email] = user;
            }
            return Task.FromResult(user);
        }
    }
}
