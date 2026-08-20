using Application.Services;
using Domain.Models;
using Xunit;

namespace Application.Tests;

public class BudgetServiceTests
{
    [Fact]
    public void SetBudget_LimitZeroOrLess_Throws()
    {
        FakeBudgetRepository repo = new FakeBudgetRepository();
        BudgetService service = new BudgetService(repo);

        Assert.Throws<ArgumentException>(() =>
            service.SetBudget(Guid.NewGuid(), Guid.NewGuid(), 2026, 8, 0)
        );
    }

    [Fact]
    public void ApplyExpense_BudgetNotFound_Throws()
    {
        FakeBudgetRepository repo = new FakeBudgetRepository();
        BudgetService service = new BudgetService(repo);

        Assert.Throws<ArgumentException>(() =>
            service.ApplyExpense(Guid.NewGuid(), Guid.NewGuid(), 2026, 8, 100)
        );
    }

    [Fact]
    public void SetBudget_NewBudget_IsCreated()
    {
        FakeBudgetRepository repo = new FakeBudgetRepository();
        BudgetService service = new BudgetService(repo);

        Guid userId = Guid.NewGuid();
        Guid categoryId = Guid.NewGuid();
        service.SetBudget(userId, categoryId, 2026, 8, 1000);

        Assert.True(service.HasBudget(userId, categoryId, 2026, 8));
    }

    [Fact]
    public void SetBudget_ExistingBudget_UpdatesLimitWithoutDuplicate()
    {
        FakeBudgetRepository repo = new FakeBudgetRepository();
        BudgetService service = new BudgetService(repo);

        Guid userId = Guid.NewGuid();
        Guid categoryId = Guid.NewGuid();
        service.SetBudget(userId, categoryId, 2026, 8, 1000);
        service.SetBudget(userId, categoryId, 2026, 8, 2000);

        Budget? budget = repo.GetByUserCategoryAndPeriod(userId, categoryId, 2026, 8);
        Assert.NotNull(budget);
        Assert.Equal(2000, budget.Limit);
        Assert.Single(repo.GetAllByUser(userId));
    }
}