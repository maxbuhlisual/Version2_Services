using System.Globalization;
using Dapper;
using Domain.Interfaces;
using Domain.Models;
using Infrastructure.Data;

namespace Infrastructure.Repositories;

public class SqliteExpenseBudgetWriter(SqliteConnectionFactory connections) : IExpenseBudgetWriter
{
    public void Save(Expense expense)
    {
        using var connection = connections.CreateConnection();
        connection.Open();
        // BEGIN IMMEDIATE: блокировка записи получена до чтения бюджета.
        using var transaction = connection.BeginTransaction(deferred: false);
        SqliteExpenseRepository.Add(connection, transaction, expense);
        var budget = connection.QueryFirstOrDefault<BudgetSpent>("""
            SELECT Id, Spent FROM Budgets
            WHERE UserId = @UserId AND CategoryId = @CategoryId AND Year = @Year AND Month = @Month;
            """, new { UserId = expense.UserId.ToString("D"), CategoryId = expense.CategoryId.ToString("D"),
                Year = expense.Date.Year, Month = expense.Date.Month }, transaction);
        if (budget != null)
        {
            decimal spent = decimal.Parse(budget.Spent, CultureInfo.InvariantCulture) + expense.Amount;
            connection.Execute("UPDATE Budgets SET Spent = @Spent WHERE Id = @Id;",
                new { budget.Id, Spent = spent.ToString(CultureInfo.InvariantCulture) }, transaction);
        }
        transaction.Commit();
        // При исключении Dispose транзакции откатывает вставку и обновление.
    }

    private sealed class BudgetSpent
    {
        public string Id { get; set; } = string.Empty;
        public string Spent { get; set; } = string.Empty;
    }
}
