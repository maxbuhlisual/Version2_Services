using Domain.Models;

namespace Domain.Interfaces;

public interface IUserRepository
{
    void Add(User user);
    User? GetById(Guid id);
    User? GetByEmail(string email);
}