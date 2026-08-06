using Domain.Models;
using Domain.Interfaces;
namespace Application.Services;

public class BudgetService
{
    private readonly IBudgetRepository _budgets;
    public BudgetService(IBudgetRepository budgets)
    {
        _budgets = budgets;
    }
    
    public void SetBudget(Guid userId, Guid categoryId, int year, int month, decimal limit)
    {
        Budget? existing = _budgets.GetByUserCategoryAndPeriod(userId, categoryId, year, month);

        if (existing != null)
        {
            existing.Limit = limit;
            _budgets.Add(existing);   
        }
        else
        {
            
            Budget budget = new Budget()
            {
                Spent = 0,
                Id = Guid.NewGuid(),
                UserId = userId,
                CategoryId = categoryId,
                Year = year,
                Month = month,
                Limit = limit
            };
            _budgets.Add(budget);
        }
    }
    
    public void ApplyExpense(Guid userId, Guid categoryId, int year, int month, decimal amount)
    {
        Budget? budget = _budgets.GetByUserCategoryAndPeriod(userId, categoryId, year, month);

        if (budget != null)
        {
            budget.Spent += amount;
            _budgets.Add(budget);
        }
    }
}