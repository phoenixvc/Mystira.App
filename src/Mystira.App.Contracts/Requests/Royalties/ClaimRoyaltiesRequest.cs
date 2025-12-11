namespace Mystira.App.Contracts.Requests.Royalties;

/// <summary>
/// Request to claim accumulated royalties
/// </summary>
public class ClaimRoyaltiesRequest
{
    /// <summary>
    /// The wallet address of the contributor claiming royalties
    /// </summary>
    public string ContributorWallet { get; set; } = string.Empty;
}
