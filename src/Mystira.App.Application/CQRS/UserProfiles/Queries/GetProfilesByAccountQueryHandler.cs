using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Application.Specifications;
using Mystira.Domain.Models;

namespace Mystira.App.Application.CQRS.UserProfiles.Queries;

/// <summary>
/// Wolverine handler for GetProfilesByAccountQuery.
/// Retrieves all profiles associated with a specific account.
/// </summary>
public static class GetProfilesByAccountQueryHandler
{
    /// <summary>
    /// Handles the GetProfilesByAccountQuery by retrieving profiles for an account.
    /// Wolverine injects dependencies as method parameters.
    /// </summary>
    public static async Task<List<UserProfile>> Handle(
        GetProfilesByAccountQuery request,
        IUserProfileRepository repository,
        ILogger logger,
        CancellationToken ct)
    {
        if (string.IsNullOrEmpty(request.AccountId))
        {
            throw new ArgumentException("AccountId is required");
        }

        var spec = new ProfilesByAccountSpec(request.AccountId);
        var profiles = await repository.ListAsync(spec);

        logger.LogDebug("Retrieved {Count} profiles for account {AccountId}",
            profiles.Count(), request.AccountId);

        return profiles.ToList();
    }
}
