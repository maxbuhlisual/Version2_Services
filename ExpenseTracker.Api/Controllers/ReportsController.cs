using Microsoft.AspNetCore.Mvc;
using Application.Services;

namespace ExpenseTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReportsController : ControllerBase
{
    private readonly ReportService _reportService;

    public ReportsController(ReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpGet("monthly")]
    public IActionResult GetMonthlyReport([FromQuery] Guid userId, [FromQuery] int year, [FromQuery] int month)
    {
        string report = _reportService.GenerateMonthlyReport(userId, year, month);
        return Ok(report);
    }
}