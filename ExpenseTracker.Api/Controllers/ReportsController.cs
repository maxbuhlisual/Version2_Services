using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Application.Services;

namespace ExpenseTracker.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ReportsController : ControllerBase
{
    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private readonly ReportService _reportService;

    public ReportsController(ReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpGet("monthly")]
    public IActionResult GetMonthlyReport([FromQuery] Guid userId, [FromQuery] int year, [FromQuery] int month)
    {
        if (userId != CurrentUserId) return Forbid();
        string report = _reportService.GenerateMonthlyReport(CurrentUserId, year, month);
        return Ok(report);
    }
}