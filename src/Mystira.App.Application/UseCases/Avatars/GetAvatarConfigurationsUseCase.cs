using Microsoft.Extensions.Logging;
using Mystira.App.Contracts.Responses.Media;
using Mystira.App.Domain.Models;
using Mystira.App.Infrastructure.Data.Repositories;

namespace Mystira.App.Application.UseCases.Avatars;

/// <summary>
/// Use case for retrieving all avatar configurations
/// </summary>
public class GetAvatarConfigurationsUseCase
{
    private readonly IAvatarConfigurationFileRepository _repository;
    private readonly ILogger<GetAvatarConfigurationsUseCase> _logger;

    public GetAvatarConfigurationsUseCase(
        IAvatarConfigurationFileRepository repository,
        ILogger<GetAvatarConfigurationsUseCase> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<AvatarResponse> ExecuteAsync()
    {
        var configFile = await _repository.GetAsync();

        var response = new AvatarResponse
        {
            AgeGroupAvatars = configFile?.AgeGroupAvatars ?? new Dictionary<string, List<string>>()
        };

        // Ensure all age groups are present
        var allAgeGroups = AgeGroup.ValueMap.Values.Select(a => a.Value).ToList();
        foreach (var ageGroup in allAgeGroups)
        {
            if (!response.AgeGroupAvatars.ContainsKey(ageGroup))
            {
                response.AgeGroupAvatars[ageGroup] = new List<string>();
            }
        }

        _logger.LogInformation("Retrieved avatar configurations for {Count} age groups", response.AgeGroupAvatars.Count);
        return response;
    }
}

