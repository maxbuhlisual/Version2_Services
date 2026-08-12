namespace ExpenseTracker.Api.Models;

public class SetBudgetRequest
{
    public required Guid UserId { get; set; }
    public required Guid CategoryId { get; set; }
    public required int Year { get; set; }
    public required int Month { get; set; }
    public required decimal Limit { get; set; }
}