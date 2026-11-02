using Domain.Models;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.Data.Sqlite;

namespace Application.Tests;

public sealed class SqliteUserRepositoryTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "ExpenseTrackerTests", Guid.NewGuid().ToString("N"));
    private readonly SqliteConnectionFactory _connections;
    private readonly SqliteUserRepository _repository;

    public SqliteUserRepositoryTests()
    {
        // Pooling=False позволяет удалить временную базу после закрытия соединений.
        _connections = new SqliteConnectionFactory("Data Source=users.db;Pooling=False", _directory);
        new SqliteDatabaseInitializer(_connections).Initialize();
        _repository = new SqliteUserRepository(_connections);
    }

    [Fact]
    public void Add_ReadByIdAndEmail_RestoresEveryFieldAndGuid()
    {
        User user = CreateUser();
        _repository.Add(user);

        AssertUser(user, _repository.GetById(user.Id));
        AssertUser(user, _repository.GetByEmail(user.Email));
    }

    [Fact]
    public void MissingUser_ReturnsNull()
    {
        Assert.Null(_repository.GetById(Guid.NewGuid()));
        Assert.Null(_repository.GetByEmail("missing@example.invalid"));
    }

    [Fact]
    public void ReinitializeAndNewFactory_PreserveUser()
    {
        User user = CreateUser();
        _repository.Add(user);

        var connections = new SqliteConnectionFactory("Data Source=users.db;Pooling=False", _directory);
        new SqliteDatabaseInitializer(connections).Initialize();
        AssertUser(user, new SqliteUserRepository(connections).GetById(user.Id));
    }

    [Fact]
    public void SqlLikeText_IsStoredLiterallyAndDoesNotChangeQuery()
    {
        User user = CreateUser();
        user.Email = "test' OR 1=1; --@example.invalid";
        user.Name = "О'Коннор; DROP TABLE Users; --";
        _repository.Add(user);

        AssertUser(user, _repository.GetByEmail(user.Email));
        Assert.Null(_repository.GetByEmail("' OR 1=1 --"));
        AssertUser(user, _repository.GetById(user.Id));
    }

    [Fact]
    public void Add_ExistingId_UpdatesWithoutDuplicate()
    {
        User user = CreateUser();
        _repository.Add(user);
        string previousEmail = user.Email;
        user.Email = "updated@example.invalid";
        user.Name = "Обновлённое имя";
        user.PasswordHash = "updated-test-hash";
        _repository.Add(user);

        AssertUser(user, _repository.GetById(user.Id));
        AssertUser(user, _repository.GetByEmail(user.Email));
        Assert.Null(_repository.GetByEmail(previousEmail));
        using var connection = _connections.CreateConnection();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM Users;";
        Assert.Equal(1L, (long)command.ExecuteScalar()!);
    }

    private static User CreateUser() => new()
    {
        Id = Guid.NewGuid(),
        Email = "learner@example.invalid",
        Name = "Учебный пользователь",
        PasswordHash = "fictional-test-hash"
    };

    private static void AssertUser(User expected, User? actual)
    {
        Assert.NotNull(actual);
        Assert.Equal(expected.Id, actual.Id);
        Assert.Equal(expected.Email, actual.Email);
        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.PasswordHash, actual.PasswordHash);
    }

    public void Dispose()
    {
        Directory.Delete(_directory, recursive: true);
    }
}
