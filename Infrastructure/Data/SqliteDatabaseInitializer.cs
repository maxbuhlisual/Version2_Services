using Dapper;

namespace Infrastructure.Data;

public class SqliteDatabaseInitializer(SqliteConnectionFactory connections)
{
    public void Initialize()
    {
        using var connection = connections.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();
        connection.Execute("""
            CREATE TABLE IF NOT EXISTS Users (
                Id TEXT NOT NULL PRIMARY KEY,
                Email TEXT NOT NULL,
                Name TEXT NOT NULL,
                PasswordHash TEXT NOT NULL
            );
            """, transaction: transaction);
        long conflicts = connection.QuerySingle<long>("""
            SELECT COUNT(*) FROM (
                SELECT Email FROM Users GROUP BY Email COLLATE EMAIL_NORMALIZED HAVING COUNT(*) > 1
            );
            """, transaction: transaction);
        if (conflicts > 0)
            throw new InvalidOperationException("Обновление Users остановлено: найдены конфликтующие email. Данные сохранены; требуется ручное разрешение конфликтов.");
        connection.Execute("CREATE UNIQUE INDEX IF NOT EXISTS UX_Users_Email_Normalized ON Users(Email COLLATE EMAIL_NORMALIZED);",
            transaction: transaction);
        transaction.Commit();
    }
}
