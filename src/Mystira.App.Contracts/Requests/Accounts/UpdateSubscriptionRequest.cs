using System.ComponentModel.DataAnnotations;
using Mystira.App.Domain.Models;

namespace Mystira.App.Contracts.Requests.Accounts;

public class UpdateSubscriptionRequest
{
    [Required]
    public SubscriptionType Type { get; set; }

    public string? ProductId { get; set; }
    public DateTime? ValidUntil { get; set; }
    public string? PurchaseToken { get; set; }
    public List<string>? PurchasedScenarios { get; set; }
}

