namespace Mystira.App.Domain.Models;

public class Account
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string ExternalUserId { get; set; } = string.Empty; // External identity provider user identifier (Entra External ID)
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Role { get; set; } = "Guest"; // Default role is Guest, can be Admin
    public List<string> UserProfileIds { get; set; } = new(); // Can have multiple user profiles
    public List<string> CompletedScenarioIds { get; set; } = new(); // Scenarios completed by this account
    public SubscriptionDetails Subscription { get; set; } = new();
    public AccountSettings Settings { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastLoginAt { get; set; } = DateTime.UtcNow;
}

public class SubscriptionDetails
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public SubscriptionType Type { get; set; } = SubscriptionType.Free;
    public string ProductId { get; set; } = string.Empty; // App store product identifier
    public DateTime? ValidUntil { get; set; } // null for lifetime or free accounts
    public bool IsActive { get; set; } = true;
    public string? PurchaseToken { get; set; } // For app store verification
    public DateTime? LastVerified { get; set; } // Last time subscription was verified with app store
    public List<string> PurchasedScenarios { get; set; } = new(); // Individual scenario purchases
    public string Tier { get; set; } = "Free";
    public DateTime? StartDate { get; set; } = DateTime.UtcNow;
    public DateTime? EndDate { get; set; } = DateTime.MaxValue;

    public bool IsSubscriptionActive()
    {
        if (!IsActive)
        {
            return false;
        }

        if (ValidUntil.HasValue && ValidUntil.Value < DateTime.UtcNow)
        {
            return false;
        }

        return true;
    }
}

public class AccountSettings
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public bool CacheCredentials { get; set; } = true;
    public bool RequireAuthOnStartup { get; set; } = false;
    public string PreferredLanguage { get; set; } = "en";
    public bool NotificationsEnabled { get; set; } = true;
    public string? Theme { get; set; } = "Light";
}

public enum SubscriptionType
{
    Free,           // Limited access
    Monthly,        // Monthly subscription
    Annual,         // Annual subscription
    Lifetime,       // One-time purchase with lifetime updates
    Individual      // Individual scenario purchases
}
