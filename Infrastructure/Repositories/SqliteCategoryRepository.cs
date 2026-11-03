using Dapper;
using Domain.Interfaces;
using Domain.Models;
using Infrastructure.Data;

namespace Infrastructure.Repositories;

public class SqliteCategoryRepository(SqliteConnectionFactory connections) : ICategoryRepository
{
    public void Add(Category category)
    {
        using var connection = connections.CreateConnection();
        connection.Open();
        connection.Execute("""
            INSERT INTO Categories (Id, UserId, Name, ParentId)
            VALUES (@Id, @UserId, @Name, @ParentId)
            ON CONFLICT(Id) DO UPDATE SET
                UserId = excluded.UserId, Name = excluded.Name, ParentId = excluded.ParentId;
            """, new { Id = category.Id.ToString("D"), UserId = category.UserId.ToString("D"),
                category.Name, ParentId = category.ParentId?.ToString("D") });
    }

    public Category? GetById(Guid id)
    {
        using var connection = connections.CreateConnection();
        connection.Open();
        var row = connection.QuerySingleOrDefault<CategoryRow>(
            "SELECT Id, UserId, Name, ParentId FROM Categories WHERE Id = @Id;",
            new { Id = id.ToString("D") });
        return row == null ? null : ToCategory(row);
    }

    public List<Category> GetAllByUser(Guid userId)
    {
        using var connection = connections.CreateConnection();
        connection.Open();
        return connection.Query<CategoryRow>(
            "SELECT Id, UserId, Name, ParentId FROM Categories WHERE UserId = @UserId;",
            new { UserId = userId.ToString("D") }).Select(ToCategory).ToList();
    }

    private static Category ToCategory(CategoryRow row) => new()
    {
        Id = Guid.Parse(row.Id), UserId = Guid.Parse(row.UserId), Name = row.Name,
        ParentId = row.ParentId == null ? null : Guid.Parse(row.ParentId)
    };

    private sealed class CategoryRow
    {
        public string Id { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? ParentId { get; set; }
    }
}
