using Mystira.Domain.Models;

namespace Mystira.App.Application.CQRS.Royalties.Queries;

/// <summary>
/// Query to get claimable royalties for an IP Asset
/// </summary>
public record GetClaimableRoyaltiesQuery(string IpAssetId) : IQuery<RoyaltyBalance>;
