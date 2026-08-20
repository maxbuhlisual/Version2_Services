using Application.Services;
using Domain.Models;

namespace Application.Tests;

public class ExpenseServiceTests
{
    [Fact]
    public void AddExpense_CategoryNotFound_Throws()
    {
        FakeExpenseRepository expenseRepo = new FakeExpenseRepository();
        FakeCategoryRepository categoryRepo = new FakeCategoryRepository();
        BudgetService budgetService = new BudgetService(new FakeBudgetRepository());
        ExpenseService service = new ExpenseService(expenseRepo, categoryRepo, budgetService);

        Assert.Throws<ArgumentException>(() =>
            service.AddExpense(Guid.NewGuid(), Guid.NewGuid(), 100, new DateTime(2026, 8, 20), null)
        );
    }

    [Fact]
    public void AddExpense_ForeignCategory_Throws()
    {
        FakeExpenseRepository expenseRepo = new FakeExpenseRepository();
        FakeCategoryRepository categoryRepo = new FakeCategoryRepository();
        BudgetService budgetService = new BudgetService(new FakeBudgetRepository());
        ExpenseService service = new ExpenseService(expenseRepo, categoryRepo, budgetService);

        Guid ownerId = Guid.NewGuid();
        Guid strangerId = Guid.NewGuid();
        Category category = new Category()
        {
            Id = Guid.NewGuid(),
            UserId = ownerId,
            Name = "Food",
            ParentId = null
        };
        categoryRepo.Add(category);

        Assert.Throws<ArgumentException>(() =>
            service.AddExpense(strangerId, category.Id, 100, new DateTime(2026, 8, 20), null)
        );
    }

    [Fact]
    public void AddExpense_AmountZeroOrLess_Throws()
    {
        FakeExpenseRepository expenseRepo = new FakeExpenseRepository();
        FakeCategoryRepository categoryRepo = new FakeCategoryRepository();
        BudgetService budgetService = new BudgetService(new FakeBudgetRepository());
        ExpenseService service = new ExpenseService(expenseRepo, categoryRepo, budgetService);

        Guid userId = Guid.NewGuid();
        Category category = new Category()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = "Food",
            ParentId = null
        };
        categoryRepo.Add(category);

        Assert.Throws<ArgumentException>(() =>
            service.AddExpense(userId, category.Id, 0, new DateTime(2026, 8, 20), null)
        );
    }

    [Fact]
    public void AddExpense_ValidData_ReturnsExpense()
    {
        FakeExpenseRepository expenseRepo = new FakeExpenseRepository();
        FakeCategoryRepository categoryRepo = new FakeCategoryRepository();
        BudgetService budgetService = new BudgetService(new FakeBudgetRepository());
        ExpenseService service = new ExpenseService(expenseRepo, categoryRepo, budgetService);

        Guid userId = Guid.NewGuid();
        Category category = new Category()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = "Food",
            ParentId = null
        };
        categoryRepo.Add(category);

        Expense expense = service.AddExpense(userId, category.Id, 150, new DateTime(2026, 8, 20), null);

        Assert.Equal(150, expense.Amount);
        Assert.Equal(userId, expense.UserId);
        Assert.Single(expenseRepo.GetAllByUser(userId));
    }

    [Fact]
    public void AddExpense_WithBudget_IncreasesSpent()
    {
        FakeExpenseRepository expenseRepo = new FakeExpenseRepository();
        FakeCategoryRepository categoryRepo = new FakeCategoryRepository();
        FakeBudgetRepository budgetRepo = new FakeBudgetRepository();
        BudgetService budgetService = new BudgetService(budgetRepo);
        ExpenseService service = new ExpenseService(expenseRepo, categoryRepo, budgetService);

        Guid userId = Guid.NewGuid();
        Category category = new Category()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = "Food",
            ParentId = null
        };
        categoryRepo.Add(category);
        budgetService.SetBudget(userId, category.Id, 2026, 8, 1000);

        service.AddExpense(userId, category.Id, 100, new DateTime(2026, 8, 20), null);

        Budget? budget = budgetRepo.GetByUserCategoryAndPeriod(userId, category.Id, 2026, 8);
        Assert.NotNull(budget);
        Assert.Equal(100, budget.Spent);
    }
}