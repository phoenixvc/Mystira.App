using Microsoft.Extensions.Logging;
using Mystira.App.Application.Interfaces;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.UserProfiles.Queries;

/// <summary>
/// Handler for GetUserProfileQuery
/// Retrieves a single user profile by ID
/// </summary>
public class GetUserProfileQueryHandler : IQueryHandler<GetUserProfileQuery, UserProfile?>
{
    private readonly IUserProfileRepository _repository;
    private readonly ILogger<GetUserProfileQueryHandler> _logger;

    public GetUserProfileQueryHandler(
        IUserProfileRepository repository,
        ILogger<GetUserProfileQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<UserProfile?> Handle(
        GetUserProfileQuery request,
        CancellationToken cancellationToken)
    {
        var profile = await _repository.GetByIdAsync(request.ProfileId);

        if (profile == null)
        {
            _logger.LogDebug("Profile not found: {ProfileId}", request.ProfileId);
        }
        else
        {
            _logger.LogDebug("Retrieved profile {ProfileId}", request.ProfileId);
        }

        return profile;
    }
}
