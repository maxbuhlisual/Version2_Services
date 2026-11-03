using Microsoft.Data.Sqlite;

namespace Infrastructure.Data;

public class SqliteConnectionFactory
{
    private readonly string _connectionString;

    public SqliteConnectionFactory(string connectionString, string basePath)
    {
        var builder = new SqliteConnectionStringBuilder(connectionString);
        builder.DataSource = Path.GetFullPath(builder.DataSource, basePath);
        builder.ForeignKeys = true;
        Directory.CreateDirectory(Path.GetDirectoryName(builder.DataSource)!);
        _connectionString = builder.ToString();
    }

    // Вызывающий код открывает и освобождает собственное соединение через using.
    public SqliteConnection CreateConnection()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.CreateCollation("EMAIL_NORMALIZED", (left, right) =>
            StringComparer.Ordinal.Compare(
                Domain.Models.EmailAddress.Normalize(left),
                Domain.Models.EmailAddress.Normalize(right)));
        return connection;
    }
}
