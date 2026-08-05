using Domain.Models;
using Domain.Interfaces;
namespace Application.Services;

public class ExpenseService 
{
    private readonly IExpenseRepository _expense;
    public  ExpenseService(IExpenseRepository expense)
    {
        _expense = expense;
    }

    public Expense AddExpense(Guid userId, Guid categoryId, decimal amount, DateTime date, string? comment)
    {
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
            return expense;
    }
}