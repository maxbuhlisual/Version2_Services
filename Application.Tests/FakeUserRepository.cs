using Domain.Interfaces;
using Domain.Models;

namespace Application.Tests;

public class FakeUserRepository : IUserRepository
{
    private readonly Dictionary<Guid, User> _users = new Dictionary<Guid, User>();

    public void Add(User user)
    {
        _users[user.Id] = user;
    }

    public User? GetById(Guid id)
    {
        if (_users.ContainsKey(id))
        {
            return _users[id];
        }
        return null;
    }

    public User? GetByEmail(string email)
    {
        foreach (User user in _users.Values)
        {
            if (user.Email == email)
            {
                return user;
            }
        }
        return null;
    }
}