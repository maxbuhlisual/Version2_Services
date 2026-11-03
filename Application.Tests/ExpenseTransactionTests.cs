using Application.Services;
using Dapper;
using Domain.Models;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.Data.Sqlite;

namespace Application.Tests;

public sealed class ExpenseTransactionTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "ExpenseTransactions", Guid.NewGuid().ToString("N"));
    private readonly SqliteConnectionFactory _factory;
    private readonly Category _category = new() { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), Name = "Test" };

    public ExpenseTransactionTests()
    {
        _factory = new SqliteConnectionFactory("Data Source=test.db;Pooling=False;Default Timeout=5", _directory);
        new SqliteDatabaseInitializer(_factory).Initialize();
        new SqliteCategoryRepository(_factory).Add(_category);
        new BudgetService(new SqliteBudgetRepository(_factory)).SetBudget(_category.UserId, _category.Id, 2026, 11, 2000);
    }

    private ExpenseService CreateRequest() => new(new SqliteExpenseRepository(_factory),
        new SqliteCategoryRepository(_factory), new BudgetService(new SqliteBudgetRepository(_factory)),
        new SqliteExpenseBudgetWriter(_factory));

    [Fact]
    public void BudgetWriteFailure_RollsBackInsertedExpense()
    {
        using (var connection = _factory.CreateConnection())
        {
            connection.Open();
            connection.Execute("UPDATE Budgets SET Spent = '12.34';");
            connection.Execute("CREATE TRIGGER fail_budget BEFORE UPDATE ON Budgets BEGIN SELECT RAISE(ABORT, 'forced failure'); END;");
        }
        Assert.Throws<SqliteException>(() => CreateRequest().AddExpense(_category.UserId, _category.Id, 300,
            new DateTime(2026, 11, 3), null));
        // Независимое соединение после завершения запроса.
        using var verify = _factory.CreateConnection();
        verify.Open();
        Assert.Equal(0L, verify.QuerySingle<long>("SELECT COUNT(*) FROM Expenses;"));
        Assert.Equal("12.34", verify.QuerySingle<string>("SELECT Spent FROM Budgets;"));
    }

    [Fact]
    public async Task OverlappingRequests_PreserveBothExpensesAndTotal()
    {
        using var blocker = _factory.CreateConnection();
        blocker.Open();
        using var transaction = blocker.BeginTransaction(deferred: false);
        using var started = new CountdownEvent(2);
        Task Start(decimal amount) => Task.Factory.StartNew(() =>
        {
            var request = CreateRequest();
            started.Signal();
            return request.AddExpense(_category.UserId, _category.Id, amount, new DateTime(2026, 11, 3), null);
        }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        var first = Start(300);
        var second = Start(800);
        try
        {
            Assert.True(started.Wait(TimeSpan.FromSeconds(3)));
            await Task.Delay(200);
            Assert.False(first.IsCompleted);
            Assert.False(second.IsCompleted);
        }
        finally { transaction.Commit(); }
        await Task.WhenAll(first, second).WaitAsync(TimeSpan.FromSeconds(10));
        using var verify = _factory.CreateConnection();
        verify.Open();
        Assert.Equal(2L, verify.QuerySingle<long>("SELECT COUNT(*) FROM Expenses;"));
        Assert.Equal("1100", verify.QuerySingle<string>("SELECT Spent FROM Budgets;"));
        Assert.Equal(1100m, new SqliteExpenseRepository(_factory).GetAllByUser(_category.UserId).Sum(e => e.Amount));
    }

    [Fact]
    public void NoBudgetAndOverspending_AreAllowed()
    {
        CreateRequest().AddExpense(_category.UserId, _category.Id, 2100.12m, new DateTime(2026, 11, 3), null);
        CreateRequest().AddExpense(_category.UserId, _category.Id, 123.45m, new DateTime(2027, 1, 3), null);
        Assert.Equal(2100.12m, new SqliteBudgetRepository(_factory).GetAllByUser(_category.UserId).Single().Spent);
        Assert.Equal(2, new SqliteExpenseRepository(_factory).GetAllByUser(_category.UserId).Count);
    }

    public void Dispose() => Directory.Delete(_directory, true);
}
