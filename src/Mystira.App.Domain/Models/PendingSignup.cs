namespace Mystira.App.Domain.Models;

public class PendingSignup
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public bool IsUsed { get; set; } = false;
    public bool IsSignin { get; set; } = false; // true for signin, false for signup
    public int FailedAttempts { get; set; } = 0;
}
