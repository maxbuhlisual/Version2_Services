using System.Globalization;
using Dapper;
using Domain.Interfaces;
using Domain.Models;
using Infrastructure.Data;

namespace Infrastructure.Repositories;

public class SqliteExpenseRepository(SqliteConnectionFactory connections) : IExpenseRepository
{
    public void Add(Expense expense)
    {
        using var connection = connections.CreateConnection();
        connection.Open();
        connection.Execute("""
            INSERT INTO Expenses (Id, UserId, CategoryId, Amount, DateTicks, DateKind, Comment)
            VALUES (@Id, @UserId, @CategoryId, @Amount, @DateTicks, @DateKind, @Comment)
            ON CONFLICT(Id) DO UPDATE SET UserId = excluded.UserId, CategoryId = excluded.CategoryId,
                Amount = excluded.Amount, DateTicks = excluded.DateTicks,
                DateKind = excluded.DateKind, Comment = excluded.Comment;
            """, new { Id = expense.Id.ToString("D"), UserId = expense.UserId.ToString("D"),
                CategoryId = expense.CategoryId.ToString("D"), Amount = expense.Amount.ToString(CultureInfo.InvariantCulture),
                DateTicks = expense.Date.Ticks, DateKind = (int)expense.Date.Kind, expense.Comment });
    }

    public List<Expense> GetAllByUser(Guid userId) => Read(
        "SELECT * FROM Expenses WHERE UserId = @UserId;", new { UserId = userId.ToString("D") });

    public List<Expense> GetByUserAndDateRange(Guid userId, DateTime from, DateTime to) => Read(
        "SELECT * FROM Expenses WHERE UserId = @UserId AND DateTicks >= @From AND DateTicks <= @To;",
        new { UserId = userId.ToString("D"), From = from.Ticks, To = to.Ticks });

    private List<Expense> Read(string sql, object parameters)
    {
        using var connection = connections.CreateConnection();
        connection.Open();
        return connection.Query<ExpenseRow>(sql, parameters).Select(row => new Expense
        {
            Id = Guid.Parse(row.Id), UserId = Guid.Parse(row.UserId), CategoryId = Guid.Parse(row.CategoryId),
            Amount = decimal.Parse(row.Amount, CultureInfo.InvariantCulture),
            Date = new DateTime(row.DateTicks, (DateTimeKind)row.DateKind), Comment = row.Comment
        }).ToList();
    }

    private sealed class ExpenseRow
    {
        public string Id { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string CategoryId { get; set; } = string.Empty;
        public string Amount { get; set; } = string.Empty;
        public long DateTicks { get; set; }
        public int DateKind { get; set; }
        public string? Comment { get; set; }
    }
}
