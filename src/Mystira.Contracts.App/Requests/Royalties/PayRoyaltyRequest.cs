namespace Mystira.Contracts.App.Requests.Royalties;

/// <summary>
/// Request to pay royalties to an IP Asset
/// </summary>
public class PayRoyaltyRequest
{
    /// <summary>
    /// Amount to pay (in WIP token)
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Optional reference for the payment (e.g., order ID)
    /// </summary>
    public string? PayerReference { get; set; }
}
