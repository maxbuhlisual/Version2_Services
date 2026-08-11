using Domain.Models;
using Domain.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace Application.Services;

public class UserService
{
    private readonly IUserRepository _userRepository;

    public UserService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    private string HashPassword(string password)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(password);
        byte[] hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }

    public User CreateUser(string email, string name, string password)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email не может быть пустым");
        }
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException("Пароль не может быть пустым");
        }
        
        User user = new User()
        {
            Id = Guid.NewGuid(),
            Email = email,
            Name = name,
            PasswordHash = HashPassword(password)
        };
        _userRepository.Add(user);
        return user;
    }
}