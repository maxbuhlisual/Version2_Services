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

    //public Expense AddExpense(userId, categoryId, amount, date, comment)
    
        
    
}