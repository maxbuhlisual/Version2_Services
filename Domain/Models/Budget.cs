namespace Domain.Models;

public class Budget
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid CategoryId { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal Limit { get; set; }
    public decimal Spent { get; set; }
}