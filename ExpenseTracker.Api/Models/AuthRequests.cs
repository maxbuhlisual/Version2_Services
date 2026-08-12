namespace ExpenseTracker.Api.Models;

public class RegisterRequest
{
    public required string Email { get; set; }
    public required string Name { get; set; }
    public required string Password { get; set; }
}

public class LoginRequest
{
    public required string Email { get; set; }
    public required string Password { get; set; }
}