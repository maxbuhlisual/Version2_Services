using Domain.Interfaces;
using Domain.Models;

namespace Application.Tests;

public class FakeBudgetRepository : IBudgetRepository
{
    private readonly Dictionary<Guid, Budget> _budgets = new Dictionary<Guid, Budget>();

    public void Add(Budget budget)
    {
        _budgets[budget.Id] = budget;
    }

    public List<Budget> GetAllByUser(Guid userId)
    {
        List<Budget> result = new List<Budget>();
        foreach (Budget budget in _budgets.Values)
        {
            if (budget.UserId == userId)
            {
                result.Add(budget);
            }
        }
        return result;
    }

    public Budget? GetByUserCategoryAndPeriod(Guid userId, Guid categoryId, int year, int month)
    {
        foreach (Budget budget in _budgets.Values)
        {
            if (budget.UserId == userId && budget.CategoryId == categoryId && budget.Year == year && budget.Month == month)
            {
                return budget;
            }
        }
        return null;
    }
}