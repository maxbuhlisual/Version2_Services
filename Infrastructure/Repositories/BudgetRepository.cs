using Domain.Models;
using Domain.Interfaces;

namespace Infrastructure.Repositories;

public class BudgetRepository : IBudgetRepository
{
    public void Add(Budget budget)
    {
        InMemoryDatabase.Budgets[budget.Id] =  budget;
    }
    
    public List<Budget> GetAllByUser(Guid userId)
    {
        List<Budget> result = new();
        foreach (Budget budget in InMemoryDatabase.Budgets.Values)
        {
            if (budget.UserId == userId)
            {
                result.Add(budget);
            }
        }
        return result;
    }
    
    public Budget? GetByUserCategoryAndPeriod(Guid userId,Guid categoryId, int year, int month)
    {
        foreach (Budget budget in InMemoryDatabase.Budgets.Values)
        {
            if (budget.UserId == userId && budget.CategoryId == categoryId 
                                        && budget.Year == year && budget.Month == month)
            {
                return budget;
            }
        }
        return null;
    }
}