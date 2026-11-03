using Dapper;
using Domain.Models;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.Data.Sqlite;

namespace Application.Tests;

public sealed class SqliteMigrationTests
{
    [Fact]
    public void FreshDatabase_EnforcesOwnerAndExistenceForeignKeys()
    {
        using var db = new TemporaryDatabase();
        db.Initializer.Initialize();
        using (var connection = db.Factory.CreateConnection())
        {
            connection.Open();
            Assert.Equal(1, connection.QuerySingle<int>("PRAGMA foreign_keys;"));
            Assert.Empty(connection.Query("PRAGMA foreign_key_check;"));
        }

        var (userA, userB, categoryA, categoryB) = db.SeedTwoUsersAndCategories();
        var child = db.AddAndReturn(new Category { Id = Guid.NewGuid(), UserId = userA.Id, Name = "Child", ParentId = categoryA.Id });
        Assert.Equal(userA.Id, child.UserId);
        Assert.Null(db.Categories.GetById(categoryA.Id)!.ParentId);
        db.Expenses.Add(new Expense { Id = Guid.NewGuid(), UserId = userA.Id, CategoryId = categoryA.Id, Amount = 12.34m, Date = DateTime.UtcNow });
        db.Budgets.Add(new Budget { Id = Guid.NewGuid(), UserId = userA.Id, CategoryId = categoryA.Id, Year = 2026, Month = 11, Limit = 100m });

        AssertRejected(() => db.Categories.Add(new Category { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), Name = "No user" }));
        AssertRejected(() => db.Expenses.Add(new Expense { Id = Guid.NewGuid(), UserId = userA.Id, CategoryId = Guid.NewGuid(), Amount = 1, Date = DateTime.UtcNow }));
        AssertRejected(() => db.Expenses.Add(new Expense { Id = Guid.NewGuid(), UserId = userA.Id, CategoryId = categoryB.Id, Amount = 1, Date = DateTime.UtcNow }));
        AssertRejected(() => db.Budgets.Add(new Budget { Id = Guid.NewGuid(), UserId = userA.Id, CategoryId = categoryB.Id, Year = 2026, Month = 11, Limit = 1 }));
        AssertRejected(() => db.Categories.Add(new Category { Id = Guid.NewGuid(), UserId = userA.Id, Name = "Foreign parent", ParentId = categoryB.Id }));
        Assert.NotEqual(userA.Id, userB.Id);
    }

    [Fact]
    public void ValidLegacyDatabase_MigratesAllValuesIndexesAndIsIdempotent()
    {
        using var db = new TemporaryDatabase();
        db.CreateLegacySchema();
        var u = new User { Id = Guid.NewGuid(), Email = "legacy@example.invalid", Name = "Legacy", PasswordHash = "legacy-hash" };
        var category = new Category { Id = Guid.NewGuid(), UserId = u.Id, Name = "Root" };
        var child = new Category { Id = Guid.NewGuid(), UserId = u.Id, Name = "Child", ParentId = category.Id };
        var date = new DateTime(2025, 7, 8, 9, 10, 11, DateTimeKind.Utc).AddTicks(1234);
        var expense = new Expense { Id = Guid.NewGuid(), UserId = u.Id, CategoryId = child.Id, Amount = 123.45000000000000000001m, Date = date, Comment = "legacy" };
        var budget = new Budget { Id = Guid.NewGuid(), UserId = u.Id, CategoryId = child.Id, Year = 2025, Month = 7, Limit = 1000.00001m, Spent = 123.45000000000000000001m };
        db.InsertLegacy(u, category, child, expense, budget);

        db.Initializer.Initialize();
        var migratedUser = db.Users.GetById(u.Id)!;
        Assert.Equal(u.Id, migratedUser.Id); Assert.Equal(u.Email, migratedUser.Email);
        Assert.Equal(u.Name, migratedUser.Name); Assert.Equal(u.PasswordHash, migratedUser.PasswordHash);
        AssertCategory(category, db.Categories.GetById(category.Id)!);
        AssertCategory(child, db.Categories.GetById(child.Id)!);
        AssertExpense(expense, Assert.Single(db.Expenses.GetAllByUser(u.Id)));
        AssertBudget(budget, Assert.Single(db.Budgets.GetAllByUser(u.Id)));
        AssertSchemaAndIndexes(db);
        db.Initializer.Initialize();
        Assert.Equal(1, db.Count());
        Assert.Equal(2, db.Categories.GetAllByUser(u.Id).Count);
        Assert.Single(db.Expenses.GetAllByUser(u.Id));
        Assert.Single(db.Budgets.GetAllByUser(u.Id));
        AssertSchemaAndIndexes(db);
    }

    [Fact]
    public void InvalidLegacyDatabase_ReportsRecordAndPreservesSchemaAndRows()
    {
        using var db = new TemporaryDatabase();
        db.CreateLegacySchema();
        var owner = new User { Id = Guid.NewGuid(), Email = "legacy@example.invalid", Name = "Legacy", PasswordHash = "hash" };
        db.InsertLegacy(owner);
        const string badId = "orphan-category-id";
        using (var connection = db.Factory.CreateConnection())
        {
            connection.Open();
            connection.Execute("INSERT INTO Categories VALUES (@Id,@UserId,'Broken',NULL);", new { Id = badId, UserId = Guid.NewGuid().ToString("D") });
        }
        var before = CaptureSnapshot(db);
        var exception = Assert.Throws<InvalidOperationException>(() => db.Initializer.Initialize());
        Assert.Contains("Categories", exception.Message);
        Assert.Contains(badId, exception.Message);
        Assert.Contains("пользователь не существует", exception.Message);
        Assert.Equal(before, CaptureSnapshot(db));
    }

    private static void AssertCategory(Category expected, Category actual)
    { Assert.Equal(expected.Id, actual.Id); Assert.Equal(expected.UserId, actual.UserId); Assert.Equal(expected.Name, actual.Name); Assert.Equal(expected.ParentId, actual.ParentId); }
    private static void AssertExpense(Expense expected, Expense actual)
    { Assert.Equal(expected.Id, actual.Id); Assert.Equal(expected.UserId, actual.UserId); Assert.Equal(expected.CategoryId, actual.CategoryId); Assert.Equal(expected.Amount, actual.Amount); Assert.Equal(expected.Date, actual.Date); Assert.Equal(expected.Comment, actual.Comment); }
    private static void AssertBudget(Budget expected, Budget actual)
    { Assert.Equal(expected.Id, actual.Id); Assert.Equal(expected.UserId, actual.UserId); Assert.Equal(expected.CategoryId, actual.CategoryId); Assert.Equal(expected.Year, actual.Year); Assert.Equal(expected.Month, actual.Month); Assert.Equal(expected.Limit, actual.Limit); Assert.Equal(expected.Spent, actual.Spent); }

    private static void AssertRejected(Action write) => Assert.Throws<SqliteException>(write);

    private static void AssertSchemaAndIndexes(TemporaryDatabase db)
    {
        using var connection = db.Factory.CreateConnection();
        connection.Open();
        Assert.Empty(connection.Query("PRAGMA foreign_key_check;"));
        Assert.Equal(1, connection.QuerySingle<int>("SELECT COUNT(*) FROM pragma_index_list('Users') WHERE name='UX_Users_Email_Normalized' AND \"unique\"=1;"));
        Assert.Equal(1, connection.QuerySingle<int>("SELECT COUNT(*) FROM pragma_index_list('Categories') WHERE name='IX_Categories_UserId';"));
        Assert.Equal(1, connection.QuerySingle<int>("SELECT COUNT(*) FROM pragma_index_list('Expenses') WHERE name='IX_Expenses_UserId_DateTicks';"));
        Assert.Equal(1, connection.QuerySingle<int>("SELECT COUNT(*) FROM pragma_index_list('Budgets') WHERE name='IX_Budgets_User_Category_Period';"));
        Assert.Contains(connection.Query<dynamic>("PRAGMA foreign_key_list('Expenses');"), row => (string)row.table == "Categories");
        Assert.Contains(connection.Query<dynamic>("PRAGMA foreign_key_list('Budgets');"), row => (string)row.table == "Categories");
    }

    private static string CaptureSnapshot(TemporaryDatabase db)
    {
        using var connection = db.Factory.CreateConnection();
        connection.Open();
        var schema = connection.Query("SELECT type,name,sql FROM sqlite_master WHERE name NOT LIKE 'sqlite_%' ORDER BY type,name;").ToList();
        var users = connection.Query("SELECT * FROM Users ORDER BY Id;").ToList();
        var categories = connection.Query("SELECT * FROM Categories ORDER BY Id;").ToList();
        var expenses = connection.Query("SELECT * FROM Expenses ORDER BY Id;").ToList();
        var budgets = connection.Query("SELECT * FROM Budgets ORDER BY Id;").ToList();
        return System.Text.Json.JsonSerializer.Serialize(new { schema, users, categories, expenses, budgets });
    }

    private sealed class TemporaryDatabase : IDisposable
    {
        private readonly string _directory = Path.Combine(Path.GetTempPath(), "SqliteMigrationTests", Guid.NewGuid().ToString("N"));
        public SqliteConnectionFactory Factory { get; }
        public SqliteDatabaseInitializer Initializer { get; }
        public SqliteUserRepository Users { get; }
        public SqliteCategoryRepository Categories { get; }
        public SqliteExpenseRepository Expenses { get; }
        public SqliteBudgetRepository Budgets { get; }

        public TemporaryDatabase()
        {
            Factory = new SqliteConnectionFactory("Data Source=migration.db;Pooling=False", _directory);
            Initializer = new SqliteDatabaseInitializer(Factory);
            Users = new SqliteUserRepository(Factory);
            Categories = new SqliteCategoryRepository(Factory);
            Expenses = new SqliteExpenseRepository(Factory);
            Budgets = new SqliteBudgetRepository(Factory);
        }

        public (User, User, Category, Category) SeedTwoUsersAndCategories()
        {
            var a = NewUser("a"); var b = NewUser("b"); Users.Add(a); Users.Add(b);
            var ca = new Category { Id = Guid.NewGuid(), UserId = a.Id, Name = "A" };
            var cb = new Category { Id = Guid.NewGuid(), UserId = b.Id, Name = "B" };
            Categories.Add(ca); Categories.Add(cb); return (a, b, ca, cb);
        }

        public int Count() { using var c = Factory.CreateConnection(); c.Open(); return c.QuerySingle<int>("SELECT COUNT(*) FROM Users;"); }
        public Category AddAndReturn(Category c) { Categories.Add(c); return c; }

        public void CreateLegacySchema()
        {
            Directory.CreateDirectory(_directory);
            using var c = Factory.CreateConnection(); c.Open();
            c.Execute("""
                CREATE TABLE Users(Id TEXT NOT NULL PRIMARY KEY, Email TEXT NOT NULL, Name TEXT NOT NULL, PasswordHash TEXT NOT NULL);
                CREATE UNIQUE INDEX UX_Users_Email_Normalized ON Users(Email COLLATE NOCASE);
                CREATE TABLE Categories(Id TEXT NOT NULL PRIMARY KEY, UserId TEXT NOT NULL, Name TEXT NOT NULL, ParentId TEXT NULL);
                CREATE INDEX IX_Categories_UserId ON Categories(UserId);
                CREATE TABLE Expenses(Id TEXT NOT NULL PRIMARY KEY, UserId TEXT NOT NULL, CategoryId TEXT NOT NULL, Amount TEXT NOT NULL, DateTicks INTEGER NOT NULL, DateKind INTEGER NOT NULL, Comment TEXT NULL);
                CREATE INDEX IX_Expenses_UserId_DateTicks ON Expenses(UserId, DateTicks);
                CREATE TABLE Budgets(Id TEXT NOT NULL PRIMARY KEY, UserId TEXT NOT NULL, CategoryId TEXT NOT NULL, Year INTEGER NOT NULL, Month INTEGER NOT NULL, "Limit" TEXT NOT NULL, Spent TEXT NOT NULL);
                CREATE INDEX IX_Budgets_User_Category_Period ON Budgets(UserId, CategoryId, Year, Month);
                """);
        }

        public void InsertLegacy(User user, Category? category = null, Category? child = null, Expense? expense = null, Budget? budget = null)
        {
            using var c = Factory.CreateConnection(); c.Open();
            c.Execute("INSERT INTO Users VALUES (@Id,@Email,@Name,@PasswordHash);", new { Id=user.Id.ToString("D"), user.Email, user.Name, user.PasswordHash });
            if (category != null) c.Execute("INSERT INTO Categories VALUES (@Id,@UserId,@Name,@ParentId);", new { Id=category.Id.ToString("D"), UserId=category.UserId.ToString("D"), category.Name, ParentId=category.ParentId?.ToString("D") });
            if (child != null) c.Execute("INSERT INTO Categories VALUES (@Id,@UserId,@Name,@ParentId);", new { Id=child.Id.ToString("D"), UserId=child.UserId.ToString("D"), child.Name, ParentId=child.ParentId?.ToString("D") });
            if (expense != null) c.Execute("INSERT INTO Expenses VALUES (@Id,@UserId,@CategoryId,@Amount,@DateTicks,@DateKind,@Comment);", new { Id=expense.Id.ToString("D"), UserId=expense.UserId.ToString("D"), CategoryId=expense.CategoryId.ToString("D"), Amount=expense.Amount.ToString(System.Globalization.CultureInfo.InvariantCulture), DateTicks=expense.Date.Ticks, DateKind=(int)expense.Date.Kind, expense.Comment });
            if (budget != null) c.Execute("INSERT INTO Budgets VALUES (@Id,@UserId,@CategoryId,@Year,@Month,@Limit,@Spent);", new { Id=budget.Id.ToString("D"), UserId=budget.UserId.ToString("D"), CategoryId=budget.CategoryId.ToString("D"), budget.Year, budget.Month, Limit=budget.Limit.ToString(System.Globalization.CultureInfo.InvariantCulture), Spent=budget.Spent.ToString(System.Globalization.CultureInfo.InvariantCulture) });
        }

        public void Dispose() { if (Directory.Exists(_directory)) Directory.Delete(_directory, true); }
        private static User NewUser(string prefix) => new() { Id = Guid.NewGuid(), Email = $"{prefix}-{Guid.NewGuid():N}@test.invalid", Name = prefix, PasswordHash = "hash" };
    }
}
