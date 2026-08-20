using Domain.Interfaces;
using Domain.Models;

namespace Application.Tests;

public class FakeExpenseRepository : IExpenseRepository
{
    private readonly Dictionary<Guid, Expense> _expenses = new Dictionary<Guid, Expense>();

    public void Add(Expense expense)
    {
        _expenses[expense.Id] = expense;
    }

    public List<Expense> GetAllByUser(Guid userId)
    {
        List<Expense> result = new List<Expense>();
        foreach (Expense expense in _expenses.Values)
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
        List<Expense> result = new List<Expense>();
        foreach (Expense expense in _expenses.Values)
        {
            if (expense.UserId == userId && expense.Date >= from && expense.Date <= to)
            {
                result.Add(expense);
            }
        }
        return result;
    }
}