using Microsoft.AspNetCore.Mvc;
using Application.Services;
using ExpenseTracker.Api.Models;

namespace ExpenseTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BudgetsController : ControllerBase
{
    private readonly BudgetService _budgetService;

    public BudgetsController(BudgetService budgetService)
    {
        _budgetService = budgetService;
    }

    [HttpPost]
    public IActionResult SetBudget([FromBody] SetBudgetRequest request)
    {
        _budgetService.SetBudget(request.UserId, request.CategoryId, request.Year, request.Month, request.Limit);
        return Ok();
    }
}