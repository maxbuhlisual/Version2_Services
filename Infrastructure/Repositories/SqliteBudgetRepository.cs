using System.Globalization;
using Dapper;
using Domain.Interfaces;
using Domain.Models;
using Infrastructure.Data;

namespace Infrastructure.Repositories;

public class SqliteBudgetRepository(SqliteConnectionFactory connections) : IBudgetRepository
{
    public void Add(Budget budget)
    {
        using var connection = connections.CreateConnection();
        connection.Open();
        connection.Execute("""
            INSERT INTO Budgets (Id, UserId, CategoryId, Year, Month, "Limit", Spent)
            VALUES (@Id, @UserId, @CategoryId, @Year, @Month, @Limit, @Spent)
            ON CONFLICT(Id) DO UPDATE SET UserId = excluded.UserId, CategoryId = excluded.CategoryId,
                Year = excluded.Year, Month = excluded.Month, "Limit" = excluded."Limit", Spent = excluded.Spent;
            """, new { Id = budget.Id.ToString("D"), UserId = budget.UserId.ToString("D"),
                CategoryId = budget.CategoryId.ToString("D"), budget.Year, budget.Month,
                Limit = budget.Limit.ToString(CultureInfo.InvariantCulture),
                Spent = budget.Spent.ToString(CultureInfo.InvariantCulture) });
    }

    public List<Budget> GetAllByUser(Guid userId) => Read(
        "SELECT * FROM Budgets WHERE UserId = @UserId;", new { UserId = userId.ToString("D") });

    public Budget? GetByUserCategoryAndPeriod(Guid userId, Guid categoryId, int year, int month) => Read(
        "SELECT * FROM Budgets WHERE UserId = @UserId AND CategoryId = @CategoryId AND Year = @Year AND Month = @Month LIMIT 1;",
        new { UserId = userId.ToString("D"), CategoryId = categoryId.ToString("D"), Year = year, Month = month }).FirstOrDefault();

    private List<Budget> Read(string sql, object parameters)
    {
        using var connection = connections.CreateConnection();
        connection.Open();
        return connection.Query<BudgetRow>(sql, parameters).Select(row => new Budget
        {
            Id = Guid.Parse(row.Id), UserId = Guid.Parse(row.UserId), CategoryId = Guid.Parse(row.CategoryId),
            Year = row.Year, Month = row.Month,
            Limit = decimal.Parse(row.Limit, CultureInfo.InvariantCulture),
            Spent = decimal.Parse(row.Spent, CultureInfo.InvariantCulture)
        }).ToList();
    }

    private sealed class BudgetRow
    {
        public string Id { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string CategoryId { get; set; } = string.Empty;
        public int Year { get; set; }
        public int Month { get; set; }
        public string Limit { get; set; } = string.Empty;
        public string Spent { get; set; } = string.Empty;
    }
}
