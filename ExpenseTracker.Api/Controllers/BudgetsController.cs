using Domain.Interfaces;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Application.Services;
using ExpenseTracker.Api.Models;

namespace ExpenseTracker.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class BudgetsController : ControllerBase
{
    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private readonly BudgetService _budgetService;

    private readonly ICategoryRepository _categories;

    public BudgetsController(BudgetService budgetService, ICategoryRepository categories)
    {
        _budgetService = budgetService;
        _categories = categories;
    }

    [HttpPost]
    public IActionResult SetBudget([FromBody] SetBudgetRequest request)
    {
        if (request.UserId != CurrentUserId) return Forbid();
        var category = _categories.GetById(request.CategoryId);
        if (category == null) return BadRequest(new { error = "Категория не найдена" });
        if (category.UserId != CurrentUserId) return Forbid();
        _budgetService.SetBudget(CurrentUserId, request.CategoryId, request.Year, request.Month, request.Limit);
        return Ok();
    }

    [HttpGet("{userId}")]
    public IActionResult GetUserBudgets(Guid userId)
    {
        if (userId != CurrentUserId) return Forbid();
        return Ok(_budgetService.GetUserBudgets(CurrentUserId));
    }
}
