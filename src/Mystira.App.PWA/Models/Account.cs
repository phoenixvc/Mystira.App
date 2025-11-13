namespace Mystira.App.PWA.Models;

public class Account
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Auth0UserId { get; set; } = string.Empty; // Auth0 user identifier
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public List<string> UserProfileIds { get; set; } = new(); // Can have multiple user profiles
    public List<string> CompletedScenarioIds { get; set; } = new(); // Scenarios completed by this account
    public SubscriptionDetails Subscription { get; set; } = new();
    public AccountSettings Settings { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastLoginAt { get; set; } = DateTime.UtcNow;
}

public class SubscriptionDetails
{
    public SubscriptionType Type { get; set; } = SubscriptionType.Free;
    public string ProductId { get; set; } = string.Empty;
    public DateTime? ValidUntil { get; set; }
    public bool IsActive { get; set; } = true;
    public string? PurchaseToken { get; set; }
    public DateTime? LastVerified { get; set; }
    public List<string> PurchasedScenarios { get; set; } = new();
}

public class AccountSettings
{
    public bool CacheCredentials { get; set; } = true;
    public bool RequireAuthOnStartup { get; set; } = false;
    public string PreferredLanguage { get; set; } = "en";
    public bool NotificationsEnabled { get; set; } = true;
}

public enum SubscriptionType
{
    Free,
    Monthly,
    Annual,
    Lifetime,
    Individual
}
