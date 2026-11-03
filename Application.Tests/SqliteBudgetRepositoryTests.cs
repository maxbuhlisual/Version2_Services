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

    public SqliteBudgetRepositoryTests()
    {
        _connections = new SqliteConnectionFactory("Data Source=budgets.db;Pooling=False", _directory);
        new SqliteDatabaseInitializer(_connections).Initialize();
        _repository = new SqliteBudgetRepository(_connections);
    }

    [Fact]
    public void ReopenAndUpdate_PreserveAllFieldsAndExactMoney()
    {
        var budget = new Budget { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), CategoryId = Guid.NewGuid(),
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
        Guid user = Guid.NewGuid(), other = Guid.NewGuid(), category = Guid.NewGuid();
        service.SetBudget(user, category, 2026, 11, 1000);
        service.SetBudget(other, category, 2026, 11, 500);
        var original = _repository.GetByUserCategoryAndPeriod(user, category, 2026, 11)!;
        service.ApplyExpense(user, category, 2026, 11, 300);
        service.SetBudget(user, category, 2026, 11, 1000);
        service.ApplyExpense(user, category, 2026, 11, 800);
        var actual = Assert.Single(_repository.GetAllByUser(user));
        Assert.Equal(original.Id, actual.Id);
        Assert.Equal(1000m, actual.Limit);
        Assert.Equal(1100m, actual.Spent);
        Assert.Equal(0m, Assert.Single(_repository.GetAllByUser(other)).Spent);
    }

    public void Dispose() => Directory.Delete(_directory, recursive: true);
}
