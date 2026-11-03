using Application.Services;
using Domain.Models;
using Infrastructure.Data;
using Infrastructure.Repositories;

namespace Application.Tests;

public sealed class SqliteBudgetRepositoryTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "ExpenseTrackerBudgets", Guid.NewGuid().ToString("N"));
    private readonly SqliteConnectionFactory _connections;
    private readonly SqliteBudgetRepository _repository;
    private readonly SqliteUserRepository _users;
    private readonly SqliteCategoryRepository _categories;

    public SqliteBudgetRepositoryTests()
    {
        _connections = new SqliteConnectionFactory("Data Source=budgets.db;Pooling=False", _directory);
        new SqliteDatabaseInitializer(_connections).Initialize();
        _repository = new SqliteBudgetRepository(_connections);
        _users = new SqliteUserRepository(_connections);
        _categories = new SqliteCategoryRepository(_connections);
    }

    [Fact]
    public void ReopenAndUpdate_PreserveAllFieldsAndExactMoney()
    {
        var user = AddUser();
        var category = AddCategory(user.Id);
        var budget = new Budget { Id = Guid.NewGuid(), UserId = user.Id, CategoryId = category.Id,
            Year = 2026, Month = 11, Limit = 1000.0000000000000000000000001m, Spent = 300.00000000000000000000000001m };
        _repository.Add(budget);
        new SqliteDatabaseInitializer(_connections).Initialize();
        var reopened = new SqliteBudgetRepository(_connections);
        var actual = Assert.Single(reopened.GetAllByUser(budget.UserId));
        Assert.Equal(budget.Id, actual.Id);
        Assert.Equal(budget.UserId, actual.UserId);
        Assert.Equal(budget.CategoryId, actual.CategoryId);
        Assert.Equal(budget.Year, actual.Year);
        Assert.Equal(budget.Month, actual.Month);
        Assert.Equal(budget.Limit, actual.Limit);
        Assert.Equal(budget.Spent, actual.Spent);
        budget.Limit = 2000m;
        reopened.Add(budget);
        Assert.Equal(2000m, Assert.Single(reopened.GetAllByUser(budget.UserId)).Limit);
        Assert.Empty(reopened.GetAllByUser(Guid.NewGuid()));
        Assert.Null(reopened.GetByUserCategoryAndPeriod(Guid.NewGuid(), budget.CategoryId, 2026, 11));
        Assert.Null(reopened.GetByUserCategoryAndPeriod(budget.UserId, Guid.NewGuid(), 2026, 11));
        Assert.Null(reopened.GetByUserCategoryAndPeriod(budget.UserId, budget.CategoryId, 2025, 11));
        Assert.Null(reopened.GetByUserCategoryAndPeriod(budget.UserId, budget.CategoryId, 2026, 12));
    }

    [Fact]
    public void Service_UpdatesExistingBudgetAndAllowsOverspending_SeparatesUsers()
    {
        var service = new BudgetService(_repository);
        var owner = AddUser(); var otherOwner = AddUser();
        var category = AddCategory(owner.Id); var otherCategory = AddCategory(otherOwner.Id);
        service.SetBudget(owner.Id, category.Id, 2026, 11, 1000);
        service.SetBudget(otherOwner.Id, otherCategory.Id, 2026, 11, 500);
        var original = _repository.GetByUserCategoryAndPeriod(owner.Id, category.Id, 2026, 11)!;
        service.ApplyExpense(owner.Id, category.Id, 2026, 11, 300);
        service.SetBudget(owner.Id, category.Id, 2026, 11, 1000);
        service.ApplyExpense(owner.Id, category.Id, 2026, 11, 800);
        var actual = Assert.Single(_repository.GetAllByUser(owner.Id));
        Assert.Equal(original.Id, actual.Id);
        Assert.Equal(1000m, actual.Limit);
        Assert.Equal(1100m, actual.Spent);
        Assert.Equal(0m, Assert.Single(_repository.GetAllByUser(otherOwner.Id)).Spent);
    }

    private User AddUser()
    {
        var user = new User { Id = Guid.NewGuid(), Email = $"{Guid.NewGuid():N}@test.invalid", Name = "Test", PasswordHash = "hash" };
        _users.Add(user); return user;
    }

    private Category AddCategory(Guid userId)
    {
        var category = new Category { Id = Guid.NewGuid(), UserId = userId, Name = "Test" };
        _categories.Add(category); return category;
    }

    public void Dispose() => Directory.Delete(_directory, recursive: true);
}
