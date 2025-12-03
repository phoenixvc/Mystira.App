using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Contracts.Responses.Media;

namespace Mystira.App.Application.CQRS.Avatars.Queries;

/// <summary>
/// Handler for retrieving avatars for a specific age group.
/// Returns empty list if age group not found.
/// </summary>
public class GetAvatarsByAgeGroupQueryHandler
    : IQueryHandler<GetAvatarsByAgeGroupQuery, AvatarConfigurationResponse?>
{
    private readonly IAvatarConfigurationFileRepository _repository;
    private readonly ILogger<GetAvatarsByAgeGroupQueryHandler> _logger;

    public GetAvatarsByAgeGroupQueryHandler(
        IAvatarConfigurationFileRepository repository,
        ILogger<GetAvatarsByAgeGroupQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<AvatarConfigurationResponse?> Handle(
        GetAvatarsByAgeGroupQuery query,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query.AgeGroup))
        {
            _logger.LogWarning("Age group is required");
            return null;
        }

        _logger.LogInformation("Retrieving avatars for age group: {AgeGroup}", query.AgeGroup);

        var configFile = await _repository.GetAsync();

        if (configFile == null || !configFile.AgeGroupAvatars.TryGetValue(query.AgeGroup, out var avatars))
        {
            _logger.LogInformation("No avatars found for age group: {AgeGroup}", query.AgeGroup);
            return new AvatarConfigurationResponse
            {
                AgeGroup = query.AgeGroup,
                AvatarMediaIds = new List<string>()
            };
        }

        _logger.LogInformation("Found {Count} avatars for age group: {AgeGroup}",
            avatars.Count, query.AgeGroup);

        return new AvatarConfigurationResponse
        {
            AgeGroup = query.AgeGroup,
            AvatarMediaIds = avatars
        };
    }
}
