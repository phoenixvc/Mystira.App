using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Application.Specifications;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.UserProfiles.Queries;

/// <summary>
/// Handler for GetProfilesByAccountQuery
/// Retrieves all profiles associated with a specific account
/// </summary>
public class GetProfilesByAccountQueryHandler : IQueryHandler<GetProfilesByAccountQuery, List<UserProfile>>
{
    private readonly IUserProfileRepository _repository;
    private readonly ILogger<GetProfilesByAccountQueryHandler> _logger;

    public GetProfilesByAccountQueryHandler(
        IUserProfileRepository repository,
        ILogger<GetProfilesByAccountQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<List<UserProfile>> Handle(
        GetProfilesByAccountQuery request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.AccountId))
        {
            throw new ArgumentException("AccountId is required");
        }

        var spec = new ProfilesByAccountSpec(request.AccountId);
        var profiles = await _repository.ListAsync(spec);

        _logger.LogDebug("Retrieved {Count} profiles for account {AccountId}",
            profiles.Count(), request.AccountId);

        return profiles.ToList();
    }
}
