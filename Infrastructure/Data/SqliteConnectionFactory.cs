using Microsoft.Data.Sqlite;

namespace Infrastructure.Data;

public class SqliteConnectionFactory
{
    private readonly string _connectionString;

    public SqliteConnectionFactory(string connectionString, string basePath)
    {
        var builder = new SqliteConnectionStringBuilder(connectionString);
        builder.DataSource = Path.GetFullPath(builder.DataSource, basePath);
        Directory.CreateDirectory(Path.GetDirectoryName(builder.DataSource)!);
        _connectionString = builder.ToString();
    }

    // Вызывающий код открывает и освобождает собственное соединение через using.
    public SqliteConnection CreateConnection()
    {
        return new SqliteConnection(_connectionString);
    }
}
