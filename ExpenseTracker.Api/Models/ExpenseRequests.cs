namespace ExpenseTracker.Api.Models;

public class AddExpenseRequest
{
    public required Guid UserId { get; set; }
    public required Guid CategoryId { get; set; }
    public required decimal Amount { get; set; }
    public required DateTime Date { get; set; }
    public string? Comment { get; set; }
}