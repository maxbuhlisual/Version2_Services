namespace Domain.Models;

public class Expense
{
    public Guid Id { get; set; }
    public Guid CategoryId { get; set; }
    public Guid UserId { get; set; }
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public string? Comment { get; set; }
}