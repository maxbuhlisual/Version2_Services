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

    [Fact]
    public void LegacyUser_LoginAfterMigration_PreservesIdAndPassword()
    {
        using var connection = _connections.CreateConnection();
        connection.Open();
        Dapper.SqlMapper.Execute(connection, "DROP INDEX UX_Users_Email_Normalized;");
        var service = new Application.Services.UserService(_repository);
        User user = service.CreateUser("old@example.invalid", "Old", "old-password");
        Dapper.SqlMapper.Execute(connection, "UPDATE Users SET Email = '  Old@Example.Invalid  ';");
        new SqliteDatabaseInitializer(_connections).Initialize();
        Assert.Equal(user.Id, service.Login(" OLD@example.invalid ", "old-password").Id);
        Assert.Throws<ArgumentException>(() => service.CreateUser(" old@EXAMPLE.invalid ", "Other", "new-password"));
        Assert.Equal(user.Id, service.Login("old@example.invalid", "old-password").Id);
        Assert.Throws<ArgumentException>(() => service.Login("old@example.invalid", "new-password"));
    }

    [Fact]
    public async Task ConcurrentRegistrations_OnlyOneSucceeds()
    {
        using var barrier = new Barrier(2);
        Task<bool> Register(string email) => Task.Run(() =>
        {
            barrier.SignalAndWait();
            try
            {
                new Application.Services.UserService(_repository).CreateUser(email, "Test", "password");
                return true;
            }
            catch (ArgumentException) { return false; }
        });
        bool[] results = await Task.WhenAll(Register("race@example.invalid"), Register(" RACE@example.invalid "));
        Assert.Single(results, result => result);
    }

    [Fact]
    public void ConflictingLegacyEmails_StopMigrationWithoutDeletingData()
    {
        using var connection = _connections.CreateConnection();
        connection.Open();
        Dapper.SqlMapper.Execute(connection, "DROP INDEX UX_Users_Email_Normalized;");
        User first = CreateUser();
        User second = CreateUser();
        _repository.Add(first);
        _repository.Add(second);
        Assert.Throws<InvalidOperationException>(() => new SqliteDatabaseInitializer(_connections).Initialize());
        Assert.NotNull(_repository.GetById(first.Id));
        Assert.NotNull(_repository.GetById(second.Id));
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
        Assert.Equal(EmailAddress.Normalize(expected.Email), actual.Email);
        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.PasswordHash, actual.PasswordHash);
    }

    public void Dispose()
    {
        Directory.Delete(_directory, recursive: true);
    }
}
