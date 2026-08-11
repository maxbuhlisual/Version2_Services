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
        if (limit <= 0)
        {
            throw new ArgumentException("Лимит должен быть больше нуля");
        }
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
        if (amount <= 0)
        {
            throw new ArgumentException("Сумма должна быть больше нуля");
        }
        
        Budget? budget = _budgets.GetByUserCategoryAndPeriod(userId, categoryId, year, month);
        if (budget == null)
        {
            throw new ArgumentException("Бюджет для этой категории и периода не найден");
        }
        
        budget.Spent += amount;
        _budgets.Add(budget);
        
    }
    public bool HasBudget(Guid userId, Guid categoryId, int year, int month)
    {
        Budget? budget = _budgets.GetByUserCategoryAndPeriod(userId, categoryId, year, month);
        return budget != null;
    }
}