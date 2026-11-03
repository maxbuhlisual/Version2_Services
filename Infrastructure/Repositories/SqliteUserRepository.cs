using Dapper;
using Domain.Interfaces;
using Domain.Models;
using Infrastructure.Data;

namespace Infrastructure.Repositories;

public class SqliteUserRepository(SqliteConnectionFactory connections) : IUserRepository
{
    public void Add(User user)
    {
        using var connection = connections.CreateConnection();
        connection.Open();
        // Как и прежний репозиторий, сохраняем или заменяем пользователя по Id.
        try
        {
        connection.Execute("""
            INSERT INTO Users (Id, Email, Name, PasswordHash)
            VALUES (@Id, @Email, @Name, @PasswordHash)
            ON CONFLICT(Id) DO UPDATE SET
                Email = excluded.Email,
                Name = excluded.Name,
                PasswordHash = excluded.PasswordHash;
            """, new { Id = user.Id.ToString("D"), Email = EmailAddress.Normalize(user.Email), user.Name, user.PasswordHash });
        }
        catch (Microsoft.Data.Sqlite.SqliteException exception) when (exception.SqliteExtendedErrorCode == 2067)
        {
            throw new ArgumentException("Пользователь с таким email уже зарегистрирован.", exception);
        }
    }

    public User? GetById(Guid id)
    {
        using var connection = connections.CreateConnection();
        connection.Open();
        var row = connection.QuerySingleOrDefault<UserRow>("""
            SELECT Id, Email, Name, PasswordHash FROM Users WHERE Id = @Id;
            """, new { Id = id.ToString("D") });
        return ToUser(row);
    }

    public User? GetByEmail(string email)
    {
        using var connection = connections.CreateConnection();
        connection.Open();
        var row = connection.QueryFirstOrDefault<UserRow>("""
            SELECT Id, Email, Name, PasswordHash FROM Users WHERE Email = @Email COLLATE EMAIL_NORMALIZED;
            """, new { Email = EmailAddress.Normalize(email) });
        return ToUser(row);
    }

    private static User? ToUser(UserRow? row)
    {
        return row == null ? null : new User
        {
            Id = Guid.Parse(row.Id),
            Email = row.Email,
            Name = row.Name,
            PasswordHash = row.PasswordHash
        };
    }

    // SQLite хранит Guid как TEXT. Преобразуем явно, без глобальных обработчиков Dapper.
    private sealed class UserRow
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
    }
}
