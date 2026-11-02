using Dapper;

namespace Infrastructure.Data;

public class SqliteDatabaseInitializer(SqliteConnectionFactory connections)
{
    public void Initialize()
    {
        using var connection = connections.CreateConnection();
        connection.Open();
        connection.Execute("""
            CREATE TABLE IF NOT EXISTS Users (
                Id TEXT NOT NULL PRIMARY KEY,
                Email TEXT NOT NULL,
                Name TEXT NOT NULL,
                PasswordHash TEXT NOT NULL
            );
            """);
    }
}
