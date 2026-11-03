using Dapper;
using Microsoft.Data.Sqlite;

namespace Infrastructure.Data;

public class SqliteDatabaseInitializer(SqliteConnectionFactory connections)
{
    public void Initialize()
    {
        using var connection = connections.CreateConnection();
        connection.Open();
        // Take the writer lock before inspecting old rows so no connection can insert between validation and migration.
        using var transaction = connection.BeginTransaction(deferred: false);
        connection.Execute("""
            CREATE TABLE IF NOT EXISTS Users (
                Id TEXT NOT NULL PRIMARY KEY,
                Email TEXT NOT NULL,
                Name TEXT NOT NULL,
                PasswordHash TEXT NOT NULL
            );
            """, transaction: transaction);
        var conflicts = connection.Query<(string Email, long Count)>("""
            SELECT Email, COUNT(*) AS Count FROM Users GROUP BY Email COLLATE EMAIL_NORMALIZED HAVING COUNT(*) > 1;
            """, transaction: transaction).ToList();
        if (conflicts.Count != 0)
            throw new InvalidOperationException("Обновление Users остановлено: конфликтующие email: " + string.Join(", ", conflicts.Select(x => x.Email)));

        EnsureLegacyTables(connection, transaction);
        var violations = new List<string>();
        AddViolations(connection, transaction, violations, "Categories", "SELECT c.Id, 'пользователь не существует' FROM Categories c LEFT JOIN Users u ON u.Id=c.UserId WHERE u.Id IS NULL UNION ALL SELECT c.Id, 'родительская категория отсутствует или принадлежит другому пользователю' FROM Categories c LEFT JOIN Categories p ON p.Id=c.ParentId AND p.UserId=c.UserId WHERE c.ParentId IS NOT NULL AND p.Id IS NULL");
        AddViolations(connection, transaction, violations, "Expenses", "SELECT e.Id, 'пользователь не существует' FROM Expenses e LEFT JOIN Users u ON u.Id=e.UserId WHERE u.Id IS NULL UNION ALL SELECT e.Id, 'категория отсутствует или принадлежит другому пользователю' FROM Expenses e LEFT JOIN Categories c ON c.Id=e.CategoryId AND c.UserId=e.UserId WHERE c.Id IS NULL");
        AddViolations(connection, transaction, violations, "Budgets", "SELECT b.Id, 'пользователь не существует' FROM Budgets b LEFT JOIN Users u ON u.Id=b.UserId WHERE u.Id IS NULL UNION ALL SELECT b.Id, 'категория отсутствует или принадлежит другому пользователю' FROM Budgets b LEFT JOIN Categories c ON c.Id=b.CategoryId AND c.UserId=b.UserId WHERE c.Id IS NULL");
        if (violations.Count != 0)
            throw new InvalidOperationException("Миграция SQLite остановлена; таблица, ID и причина: " + string.Join("; ", violations));

        var fkEnabled = connection.QuerySingle<int>("PRAGMA foreign_keys;", transaction: transaction);
        // SQLite only permits changing foreign_keys outside a transaction; use legacy_alter_table-independent table swap.
        connection.Execute("PRAGMA defer_foreign_keys = ON;", transaction: transaction);
        Rebuild(connection, transaction, fkEnabled);
        var fkProblems = connection.Query("PRAGMA foreign_key_check;", transaction: transaction).ToList();
        if (fkProblems.Count != 0)
            throw new InvalidOperationException("PRAGMA foreign_key_check обнаружил нарушения: " + System.Text.Json.JsonSerializer.Serialize(fkProblems));
        transaction.Commit();
    }

    private static void EnsureLegacyTables(SqliteConnection c, SqliteTransaction t)
    {
        c.Execute("""
            CREATE TABLE IF NOT EXISTS Categories (Id TEXT NOT NULL PRIMARY KEY, UserId TEXT NOT NULL, Name TEXT NOT NULL, ParentId TEXT NULL);
            CREATE TABLE IF NOT EXISTS Expenses (Id TEXT NOT NULL PRIMARY KEY, UserId TEXT NOT NULL, CategoryId TEXT NOT NULL, Amount TEXT NOT NULL, DateTicks INTEGER NOT NULL, DateKind INTEGER NOT NULL, Comment TEXT NULL);
            CREATE TABLE IF NOT EXISTS Budgets (Id TEXT NOT NULL PRIMARY KEY, UserId TEXT NOT NULL, CategoryId TEXT NOT NULL, Year INTEGER NOT NULL, Month INTEGER NOT NULL, "Limit" TEXT NOT NULL, Spent TEXT NOT NULL);
            """, transaction: t);
    }

    private static void AddViolations(SqliteConnection c, SqliteTransaction t, List<string> result, string table, string sql)
    {
        foreach (var row in c.Query<(string Id, string Reason)>(sql, transaction: t)) result.Add($"{table}, {row.Id}: {row.Reason}");
    }

    private static void Rebuild(SqliteConnection c, SqliteTransaction t, int fkEnabled)
    {
        c.Execute("""
            CREATE TABLE Users_new (Id TEXT NOT NULL PRIMARY KEY, Email TEXT NOT NULL, Name TEXT NOT NULL, PasswordHash TEXT NOT NULL);
            CREATE TABLE Categories_new (
                Id TEXT NOT NULL PRIMARY KEY, UserId TEXT NOT NULL, Name TEXT NOT NULL, ParentId TEXT NULL,
                UNIQUE(UserId, Id),
                FOREIGN KEY(UserId) REFERENCES Users_new(Id),
                FOREIGN KEY(UserId, ParentId) REFERENCES Categories_new(UserId, Id));
            CREATE TABLE Expenses_new (
                Id TEXT NOT NULL PRIMARY KEY, UserId TEXT NOT NULL, CategoryId TEXT NOT NULL,
                Amount TEXT NOT NULL, DateTicks INTEGER NOT NULL, DateKind INTEGER NOT NULL, Comment TEXT NULL,
                FOREIGN KEY(UserId) REFERENCES Users_new(Id),
                FOREIGN KEY(UserId, CategoryId) REFERENCES Categories_new(UserId, Id));
            CREATE TABLE Budgets_new (
                Id TEXT NOT NULL PRIMARY KEY, UserId TEXT NOT NULL, CategoryId TEXT NOT NULL,
                Year INTEGER NOT NULL, Month INTEGER NOT NULL, "Limit" TEXT NOT NULL, Spent TEXT NOT NULL,
                FOREIGN KEY(UserId) REFERENCES Users_new(Id),
                FOREIGN KEY(UserId, CategoryId) REFERENCES Categories_new(UserId, Id));
            INSERT INTO Users_new SELECT * FROM Users;
            INSERT INTO Categories_new SELECT * FROM Categories;
            INSERT INTO Expenses_new SELECT * FROM Expenses;
            INSERT INTO Budgets_new SELECT * FROM Budgets;
            """, transaction: t);
        if (fkEnabled == 1) c.Execute("PRAGMA defer_foreign_keys = ON;", transaction: t);
        c.Execute("""
            DROP TABLE Expenses; DROP TABLE Budgets; DROP TABLE Categories; DROP TABLE Users;
            ALTER TABLE Users_new RENAME TO Users;
            ALTER TABLE Categories_new RENAME TO Categories;
            ALTER TABLE Expenses_new RENAME TO Expenses;
            ALTER TABLE Budgets_new RENAME TO Budgets;
            CREATE UNIQUE INDEX UX_Users_Email_Normalized ON Users(Email COLLATE EMAIL_NORMALIZED);
            CREATE INDEX IX_Categories_UserId ON Categories(UserId);
            CREATE INDEX IX_Expenses_UserId_DateTicks ON Expenses(UserId, DateTicks);
            CREATE INDEX IX_Budgets_User_Category_Period ON Budgets(UserId, CategoryId, Year, Month);
            """, transaction: t);
    }
}
