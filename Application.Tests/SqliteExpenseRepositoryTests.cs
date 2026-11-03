using Domain.Models;
using Domain.Interfaces;
using Infrastructure.Data;
using Infrastructure.Repositories;

namespace Application.Tests;

public sealed class SqliteExpenseRepositoryTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "ExpenseTrackerExpenses", Guid.NewGuid().ToString("N"));
    private readonly SqliteConnectionFactory _connections;
    private readonly SqliteExpenseRepository _repository;
    private readonly SqliteUserRepository _users;
    private readonly SqliteCategoryRepository _categories;
    private readonly Guid _userId;
    private readonly Guid _categoryId;

    public SqliteExpenseRepositoryTests()
    {
        _connections = new SqliteConnectionFactory("Data Source=expenses.db;Pooling=False", _directory);
        new SqliteDatabaseInitializer(_connections).Initialize();
        _repository = new SqliteExpenseRepository(_connections);
        _users = new SqliteUserRepository(_connections);
        _categories = new SqliteCategoryRepository(_connections);
        var user = new User { Id = Guid.NewGuid(), Email = $"{Guid.NewGuid():N}@test.invalid", Name = "Test", PasswordHash = "hash" };
        _users.Add(user); _userId = user.Id;
        var category = new Category { Id = Guid.NewGuid(), UserId = user.Id, Name = "Test" };
        _categories.Add(category); _categoryId = category.Id;
    }

    [Fact]
    public void Reopen_PreservesAllFieldsAndExactDecimal_WithoutDuplicates()
    {
        Expense expense = CreateExpense();
        expense.Amount = 123.45000000000000000000000001m;
        _repository.Add(expense);
        new SqliteDatabaseInitializer(_connections).Initialize();
        var reopened = new SqliteExpenseRepository(_connections);
        Expense actual = Assert.Single(reopened.GetAllByUser(expense.UserId));
        Assert.Equal(expense.Id, actual.Id);
        Assert.Equal(expense.UserId, actual.UserId);
        Assert.Equal(expense.CategoryId, actual.CategoryId);
        Assert.Equal(expense.Amount, actual.Amount);
        Assert.Equal(expense.Date.Ticks, actual.Date.Ticks);
        Assert.Equal(expense.Date.Kind, actual.Date.Kind);
        Assert.Equal(expense.Comment, actual.Comment);
        Assert.Empty(reopened.GetAllByUser(Guid.NewGuid()));
        expense.Comment = null;
        expense.Amount = 123.45m;
        reopened.Add(expense);
        actual = Assert.Single(reopened.GetAllByUser(expense.UserId));
        Assert.Null(actual.Comment);
        Assert.Equal(123.45m, actual.Amount);
    }

    [Fact]
    public void DateFilter_IncludesBothBounds_ExcludesOtherUsersAndOutsideRange()
    {
        Expense expense = CreateExpense();
        _repository.Add(expense);
        Assert.Single(_repository.GetByUserAndDateRange(expense.UserId, expense.Date, expense.Date));
        Assert.Empty(_repository.GetByUserAndDateRange(expense.UserId, expense.Date.AddTicks(1), DateTime.MaxValue));
        Assert.Empty(_repository.GetByUserAndDateRange(expense.UserId, DateTime.MinValue, expense.Date.AddTicks(-1)));
        Assert.Empty(_repository.GetByUserAndDateRange(Guid.NewGuid(), DateTime.MinValue, DateTime.MaxValue));
        Assert.Empty(_repository.GetByUserAndDateRange(expense.UserId, expense.Date.AddDays(1), expense.Date));
    }

    private Expense CreateExpense() => new()
    {
        Id = Guid.NewGuid(), UserId = _userId, CategoryId = _categoryId, Amount = 123.45m,
        Date = new DateTime(2026, 11, 3, 12, 34, 56, DateTimeKind.Utc).AddTicks(1234),
        Comment = "О'Коннор; DROP TABLE Expenses; --"
    };

    public void Dispose() => Directory.Delete(_directory, recursive: true);
}
