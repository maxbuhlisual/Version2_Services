using Microsoft.AspNetCore.Mvc;
using Application.Services;
using Domain.Models;
using ExpenseTracker.Api.Models;

namespace ExpenseTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly CategoryService _categoryService;

    public CategoriesController(CategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    [HttpPost]
    public IActionResult Create([FromBody] CreateCategoryRequest request)
    {
        Category category = _categoryService.CreateCategory(request.UserId, request.Name, request.ParentId);
        return Ok(category);
    }

    [HttpGet("{userId}")]
    public IActionResult GetUserCategories(Guid userId)
    {
        List<Category> categories = _categoryService.GetUserCategories(userId);
        return Ok(categories);
    }
}