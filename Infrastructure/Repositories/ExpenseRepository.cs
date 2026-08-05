using Domain.Models;
using Domain.Interfaces;
namespace Infrastructure.Repositories;

public class ExpenseRepository : IExpenseRepository
{
    public void Add(Expense expense)
    {
        InMemoryDatabase.Expenses[expense.Id] = expense;
    }

    public List<Expense> GetAllByUser(Guid userId)
    {
        List<Expense> result = new();
        foreach (Expense expense in InMemoryDatabase.Expenses.Values)
        {
            if (expense.UserId == userId)
            {
                result.Add(expense);
            }
        }
        return result;
    }

    public List<Expense> GetByUserAndDateRange(Guid userId, DateTime from, DateTime to)
    {
        List<Expense> result = new();
        foreach (Expense expense in InMemoryDatabase.Expenses.Values)
        {
            if (expense.UserId == userId && expense.Date >= from && expense.Date <= to)
            {
                result.Add(expense);
            }
        }
        return result;
    }
}