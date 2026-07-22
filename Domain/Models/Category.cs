namespace Domain.Models;

public class Category
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public required string Name { get; set; }
    public Guid? ParentId { get; set; }
}