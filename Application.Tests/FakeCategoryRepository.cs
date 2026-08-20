using Domain.Interfaces;
using Domain.Models;

namespace Application.Tests;

public class FakeCategoryRepository : ICategoryRepository
{
    private readonly Dictionary<Guid, Category> _categories = new Dictionary<Guid, Category>();

    public void Add(Category category)
    {
        _categories[category.Id] = category;
    }

    public Category? GetById(Guid id)
    {
        if (_categories.ContainsKey(id))
        {
            return _categories[id];
        }
        return null;
    }

    public List<Category> GetAllByUser(Guid userId)
    {
        List<Category> result = new List<Category>();
        foreach (Category category in _categories.Values)
        {
            if (category.UserId == userId)
            {
                result.Add(category);
            }
        }
        return result;
    }
}