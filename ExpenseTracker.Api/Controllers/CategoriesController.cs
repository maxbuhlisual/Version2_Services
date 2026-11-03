using Domain.Interfaces;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Application.Services;
using Domain.Models;
using ExpenseTracker.Api.Models;

namespace ExpenseTracker.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private readonly CategoryService _categoryService;

    private readonly ICategoryRepository _categories;

    public CategoriesController(CategoryService categoryService, ICategoryRepository categories)
    {
        _categoryService = categoryService;
        _categories = categories;
    }

    [HttpPost]
    public IActionResult Create([FromBody] CreateCategoryRequest request)
    {
        if (request.UserId != CurrentUserId) return Forbid();
        if (request.ParentId is Guid parentId)
        {
            var parent = _categories.GetById(parentId);
            if (parent == null) return BadRequest(new { error = "Родительская категория не найдена" });
            if (parent.UserId != CurrentUserId) return Forbid();
        }
        Category category = _categoryService.CreateCategory(CurrentUserId, request.Name, request.ParentId);
        return Ok(category);
    }

    [HttpGet("{userId}")]
    public IActionResult GetUserCategories(Guid userId)
    {
        if (userId != CurrentUserId) return Forbid();
        List<Category> categories = _categoryService.GetUserCategories(CurrentUserId);
        return Ok(categories);
    }
}