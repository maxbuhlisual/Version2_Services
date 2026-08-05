using Domain.Models;

namespace Domain.Interfaces;

public interface ICategoryRepository
{
    void Add(Category category);
    Category? GetById(Guid id);
    List<Category> GetAllByUser(Guid userId);
}