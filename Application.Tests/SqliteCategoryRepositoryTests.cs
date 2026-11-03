using Domain.Models;
using Domain.Interfaces;
using Infrastructure.Data;
using Infrastructure.Repositories;

namespace Application.Tests;

public sealed class SqliteCategoryRepositoryTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "ExpenseTrackerCategories", Guid.NewGuid().ToString("N"));
    private readonly SqliteConnectionFactory _connections;
    private readonly SqliteCategoryRepository _repository;
    private readonly SqliteUserRepository _users;

    public SqliteCategoryRepositoryTests()
    {
        _connections = new SqliteConnectionFactory("Data Source=categories.db;Pooling=False", _directory);
        new SqliteDatabaseInitializer(_connections).Initialize();
        _repository = new SqliteCategoryRepository(_connections);
        _users = new SqliteUserRepository(_connections);
    }

    [Fact]
    public void RoundTrip_PreservesIdsNullableParentAndSqlLikeName()
    {
        var user = CreateUser(); _users.Add(user);
        var parent = new Category { Id = Guid.NewGuid(), UserId = user.Id, Name = "Родитель" };
        var child = new Category { Id = Guid.NewGuid(), UserId = parent.UserId, ParentId = parent.Id, Name = "О'Коннор; DROP TABLE Categories; --" };
        _repository.Add(parent);
        _repository.Add(child);
        Assert.Null(_repository.GetById(parent.Id)!.ParentId);
        AssertCategory(child, _repository.GetById(child.Id)!);
        Assert.Equal(2, _repository.GetAllByUser(parent.UserId).Count);
        Assert.Empty(_repository.GetAllByUser(Guid.NewGuid()));
        Assert.Null(_repository.GetById(Guid.NewGuid()));
    }

    [Fact]
    public void Reinitialize_PreservesDataWithoutDuplicates_AndAddUpdates()
    {
        var user = CreateUser(); _users.Add(user);
        var category = new Category { Id = Guid.NewGuid(), UserId = user.Id, Name = "Еда" };
        _repository.Add(category);
        new SqliteDatabaseInitializer(_connections).Initialize();
        var reopened = new SqliteCategoryRepository(new SqliteConnectionFactory("Data Source=categories.db;Pooling=False", _directory));
        AssertCategory(category, Assert.Single(reopened.GetAllByUser(category.UserId)));
        category.Name = "Продукты";
        reopened.Add(category);
        AssertCategory(category, Assert.Single(reopened.GetAllByUser(category.UserId)));
    }

    private static User CreateUser() => new() { Id = Guid.NewGuid(), Email = $"{Guid.NewGuid():N}@test.invalid", Name = "Test", PasswordHash = "hash" };

    private static void AssertCategory(Category expected, Category actual)
    {
        Assert.Equal(expected.Id, actual.Id);
        Assert.Equal(expected.UserId, actual.UserId);
        Assert.Equal(expected.ParentId, actual.ParentId);
        Assert.Equal(expected.Name, actual.Name);
    }

    public void Dispose() => Directory.Delete(_directory, recursive: true);
}
