using DocuFlow.Application.Interfaces;
using DocuFlow.Domain.Entities;

namespace DocuFlow.Infrastructure.Persistence;

public class InMemoryUserRepository : IUserRepository
{
    private readonly List<User> _users = new();

    public Task<User?> GetByEmailAsync(string email)
    {
        return Task.FromResult(_users.FirstOrDefault(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase)));
    }

    public Task<User?> GetByIdAsync(Guid id)
    {
        return Task.FromResult(_users.FirstOrDefault(u => u.Id == id));
    }

    public Task CreateAsync(User user)
    {
        _users.Add(user);
        return Task.CompletedTask;
    }
}
