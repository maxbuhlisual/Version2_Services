using Microsoft.AspNetCore.Mvc;
using Application.Services;
using Domain.Models;
using ExpenseTracker.Api.Models;

namespace ExpenseTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExpensesController : ControllerBase
{
    private readonly ExpenseService _expenseService;

    public ExpensesController(ExpenseService expenseService)
    {
        _expenseService = expenseService;
    }

    [HttpPost]
    public IActionResult Add([FromBody] AddExpenseRequest request)
    {
        Expense expense = _expenseService.AddExpense(request.UserId, request.CategoryId, request.Amount, request.Date, request.Comment);
        return Ok(expense);
    }
}