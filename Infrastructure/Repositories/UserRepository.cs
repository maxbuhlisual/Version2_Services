using   Domain.Models;
using Domain.Interfaces;
namespace Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    public void Add(User user)
    {
        InMemoryDatabase.Users[user.Id] = user;
    }

    public User? GetById(Guid id)
    {
        if (InMemoryDatabase.Users.ContainsKey(id))
        {
            return InMemoryDatabase.Users[id];
        }

        return null;
    }

    public User? GetByEmail(string email)
    {
        foreach (User user in InMemoryDatabase.Users.Values)
        {
            if (user.Email == email)
            {
                return user;
            }
        }

        return null;
    }
}