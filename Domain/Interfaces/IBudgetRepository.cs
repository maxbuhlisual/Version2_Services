using Domain.Models;

namespace Domain.Interfaces;

public interface IBudgetRepository
{
    void Add(Budget budget);
    List<Budget> GetAllByUser(Guid userId);
    Budget? GetByUserCategoryAndPeriod(Guid userId, Guid categoryId, int year, int month);
}