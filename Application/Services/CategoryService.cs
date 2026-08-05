using Domain.Models;
using Domain.Interfaces;

namespace Application.Services;

public class CategoryService
{
    private readonly ICategoryRepository _categories;
    public CategoryService(ICategoryRepository category)
    {
        _categories = category;
    }

    public Category CreateCategory(Guid userId, string name, Guid? parentId)
    {
        Category category = new Category()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = name,
            ParentId = parentId
        };
        _categories.Add(category);
        return category;
    }

    public List<Category> GetUserCategories(Guid userId)
    {
        return _categories.GetAllByUser(userId);
    }
}