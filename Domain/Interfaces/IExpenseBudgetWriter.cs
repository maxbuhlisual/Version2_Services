using Domain.Models;

namespace Domain.Interfaces;

public interface IExpenseBudgetWriter
{
    void Save(Expense expense);
}
