namespace ExpenseTracker.Api.Models;

public class CreateCategoryRequest
{
    public required Guid UserId { get; set; }
    public required string Name { get; set; }
    public Guid? ParentId { get; set; }
}