using Application.Services;
using Domain.Models;
using Xunit;

namespace Application.Tests;

public class UserServiceTests
{
    [Fact]
    public void CreateUser_EmptyEmail_Throws()
    {
        FakeUserRepository repo = new FakeUserRepository();
        UserService service = new UserService(repo);

        Assert.Throws<ArgumentException>(() =>
            service.CreateUser("", "Max", "password123")
        );
    }

    [Fact]
    public void CreateUser_EmptyPassword_Throws()
    {
        FakeUserRepository repo = new FakeUserRepository();
        UserService service = new UserService(repo);

        Assert.Throws<ArgumentException>(() =>
            service.CreateUser("max@example.com", "Max", "")
        );
    }

    [Fact]
    public void CreateUser_ValidData_ReturnsUserWithHashedPassword()
    {
        FakeUserRepository repo = new FakeUserRepository();
        UserService service = new UserService(repo);

        User user = service.CreateUser("max@example.com", "Max", "password123");

        Assert.Equal("max@example.com", user.Email);
        Assert.NotEqual("password123", user.PasswordHash);
        Assert.NotNull(repo.GetByEmail("max@example.com"));
    }
}