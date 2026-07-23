using Domain.Models;
namespace Infrastructure.Repositories;

public class CategoryRepository
{
    public void Add(Category category)
    {
        InMemoryDatabase.Categories[category.Id] = category;
    }

    public Category? GetById(Guid id)
    {
        if (InMemoryDatabase.Categories.ContainsKey(id))
        {
            return InMemoryDatabase.Categories[id];
        }
        return null;
    }

    public List<Category> GetAllByUser(Guid userId)
    {
        List<Category> result = new();
        foreach (Category category in InMemoryDatabase.Categories.Values)
        {
            if (category.UserId == userId)
            {
                result.Add(category);
            }
        }
        return result;
    }
}