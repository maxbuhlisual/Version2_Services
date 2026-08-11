using Domain.Models;
using Domain.Interfaces;
namespace Application.Services;

public class ExpenseService 
{
    private readonly IExpenseRepository _expense;
    private readonly ICategoryRepository _categories;
    private readonly BudgetService _budgetService;

    public ExpenseService(IExpenseRepository expense, ICategoryRepository categories, BudgetService budgetService)
    {
        _expense = expense;
        _categories = categories;
        _budgetService = budgetService;
    }

    public Expense AddExpense(Guid userId, Guid categoryId, decimal amount, DateTime date, string? comment)
    {
        Category? category = _categories.GetById(categoryId);

        if (category == null)
        {
            throw new ArgumentException("Категория не найдена");
        }
        if (category.UserId != userId)
        {
            throw new ArgumentException("Категория не принадлежит этому пользователю");
        }
        if (amount <= 0)
        {
            throw new ArgumentException("Сумма должна быть больше нуля");
        }
        
        Expense expense = new Expense()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CategoryId = categoryId,
            Amount = amount,
            Date = date,
            Comment = comment
        };
        _expense.Add(expense);

        bool budgetExists = _budgetService.HasBudget(userId, categoryId, date.Year, date.Month);
        if (budgetExists)
        {
            _budgetService.ApplyExpense(userId, categoryId, date.Year, date.Month, amount);
        }

        return expense;
    }
}