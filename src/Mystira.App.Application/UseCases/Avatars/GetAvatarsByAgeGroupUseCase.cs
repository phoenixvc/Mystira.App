using Microsoft.Extensions.Logging;
using Mystira.App.Contracts.Responses.Media;
using Mystira.App.Domain.Models;
using Mystira.App.Infrastructure.Data.Repositories;

namespace Mystira.App.Application.UseCases.Avatars;

/// <summary>
/// Use case for retrieving avatars for a specific age group
/// </summary>
public class GetAvatarsByAgeGroupUseCase
{
    private readonly IAvatarConfigurationFileRepository _repository;
    private readonly ILogger<GetAvatarsByAgeGroupUseCase> _logger;

    public GetAvatarsByAgeGroupUseCase(
        IAvatarConfigurationFileRepository repository,
        ILogger<GetAvatarsByAgeGroupUseCase> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<AvatarConfigurationResponse> ExecuteAsync(string ageGroup)
    {
        if (string.IsNullOrWhiteSpace(ageGroup))
        {
            throw new ArgumentException("Age group cannot be null or empty", nameof(ageGroup));
        }

        var configFile = await _repository.GetAsync();

        if (configFile == null || !configFile.AgeGroupAvatars.TryGetValue(ageGroup, out var avatars))
        {
            _logger.LogDebug("No avatars found for age group: {AgeGroup}", ageGroup);
            return new AvatarConfigurationResponse
            {
                AgeGroup = ageGroup,
                AvatarMediaIds = new List<string>()
            };
        }

        _logger.LogInformation("Retrieved {Count} avatars for age group {AgeGroup}", avatars.Count, ageGroup);
        return new AvatarConfigurationResponse
        {
            AgeGroup = ageGroup,
            AvatarMediaIds = avatars
        };
    }
}

