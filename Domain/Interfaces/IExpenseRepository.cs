using Domain.Models;

namespace Domain.Interfaces;

public interface IExpenseRepository 
{
    void Add(Expense expense);
    List<Expense> GetAllByUser(Guid userId);
    List<Expense> GetByUserAndDateRange(Guid userId, DateTime from, DateTime to);
}