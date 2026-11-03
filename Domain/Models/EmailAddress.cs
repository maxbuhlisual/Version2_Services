namespace Domain.Models;

public static class EmailAddress
{
    public static string Normalize(string email) => email.Trim().ToLowerInvariant();
}
