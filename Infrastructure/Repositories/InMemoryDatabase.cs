using   Domain.Models;
namespace Infrastructure.Repositories;

public static class InMemoryDatabase
{
    public static Dictionary<Guid, User> Users { get; } = new();
    public static Dictionary<Guid, Expense> Expenses { get; } = new();
    public static Dictionary<Guid, Category> Categories { get; } = new();
    public static Dictionary<Guid, Budget> Budgets { get; } = new();
}