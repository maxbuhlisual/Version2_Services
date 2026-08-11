using Domain.Models;
using Domain.Interfaces;

namespace Application.Services;

public class ReportService
{
    private readonly IExpenseRepository _expenses;
    private readonly ICategoryRepository _categories;

    public ReportService(IExpenseRepository expenses, ICategoryRepository categories)
    {
        _expenses = expenses;
        _categories = categories;
    }

    public string GenerateMonthlyReport(Guid userId, int year, int month)
    {
        DateTime from = new DateTime(year, month, 1);
        DateTime to = from.AddMonths(1).AddDays(-1);

        List<Expense> expenses = _expenses.GetByUserAndDateRange(userId, from, to);

        Dictionary<Guid, decimal> totalsByCategory = new();

        foreach (Expense expense in expenses)
        {
            if (totalsByCategory.ContainsKey(expense.CategoryId))
            {
                totalsByCategory[expense.CategoryId] += expense.Amount;
            }
            else
            {
                totalsByCategory[expense.CategoryId] = expense.Amount;
            }
        }

        string report = $"Отчёт за {month:D2}.{year}:\n";

        foreach (var pair in totalsByCategory)
        {
            Category? category = _categories.GetById(pair.Key);
            string categoryName = category != null ? category.Name : "Неизвестная категория";
            report += $"{categoryName}: {pair.Value:C}\n";
        }

        return report;
    }
}