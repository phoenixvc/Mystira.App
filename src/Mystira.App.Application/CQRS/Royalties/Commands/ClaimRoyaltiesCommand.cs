namespace Mystira.App.Application.CQRS.Royalties.Commands;

/// <summary>
/// Command to claim accumulated royalties for a contributor
/// </summary>
public record ClaimRoyaltiesCommand(
    string IpAssetId,
    string ContributorWallet) : ICommand<string>; // Returns transaction hash
