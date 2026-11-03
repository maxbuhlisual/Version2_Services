using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Application.Services;
using Domain.Models;
using ExpenseTracker.Api.Models;

namespace ExpenseTracker.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ExpensesController : ControllerBase
{
    private readonly ExpenseService _expenseService;

    private readonly ICategoryRepository _categories;

    public ExpensesController(ExpenseService expenseService, ICategoryRepository categories)
    {
        _expenseService = expenseService;
        _categories = categories;
    }

    [HttpGet("{userId}")]
    public IActionResult GetUserExpenses(Guid userId)
    {
        if (userId != CurrentUserId) return Forbid();
        return Ok(_expenseService.GetUserExpenses(CurrentUserId));
    }

    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost]
    public IActionResult Add([FromBody] AddExpenseRequest request)
    {
        if (request.UserId != CurrentUserId) return Forbid();
        var category = _categories.GetById(request.CategoryId);
        if (category != null && category.UserId != CurrentUserId) return Forbid();
        Expense expense = _expenseService.AddExpense(CurrentUserId, request.CategoryId, request.Amount, request.Date, request.Comment);
        return Ok(expense);
    }
}