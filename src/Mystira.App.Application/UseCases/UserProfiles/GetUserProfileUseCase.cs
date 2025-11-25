using Microsoft.Extensions.Logging;
using Mystira.App.Domain.Models;
using Mystira.App.Application.Ports.Data;

namespace Mystira.App.Application.UseCases.UserProfiles;

/// <summary>
/// Use case for retrieving a user profile by ID
/// </summary>
public class GetUserProfileUseCase
{
    private readonly IUserProfileRepository _repository;
    private readonly ILogger<GetUserProfileUseCase> _logger;

    public GetUserProfileUseCase(
        IUserProfileRepository repository,
        ILogger<GetUserProfileUseCase> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<UserProfile?> ExecuteAsync(string id)
    {
        var profile = await _repository.GetByIdAsync(id);
        if (profile == null)
        {
            _logger.LogDebug("Profile not found: {ProfileId}", id);
        }
        return profile;
    }
}

