using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Contracts.Responses.Media;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.Avatars.Queries;

/// <summary>
/// Handler for retrieving all avatar configurations.
/// Returns avatars grouped by age group, ensuring all age groups are initialized.
/// </summary>
public class GetAvatarsQueryHandler : IQueryHandler<GetAvatarsQuery, AvatarResponse>
{
    private readonly IAvatarConfigurationFileRepository _repository;
    private readonly ILogger<GetAvatarsQueryHandler> _logger;

    public GetAvatarsQueryHandler(
        IAvatarConfigurationFileRepository repository,
        ILogger<GetAvatarsQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<AvatarResponse> Handle(
        GetAvatarsQuery query,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving all avatar configurations");

        var configFile = await _repository.GetAsync();

        var response = new AvatarResponse
        {
            AgeGroupAvatars = configFile?.AgeGroupAvatars ?? new Dictionary<string, List<string>>()
        };

        // Ensure all age groups are present
        foreach (var ageGroup in AgeGroupConstants.AllAgeGroups)
        {
            response.AgeGroupAvatars.TryAdd(ageGroup, new List<string>());
        }

        _logger.LogInformation("Retrieved avatars for {Count} age groups", response.AgeGroupAvatars.Count);
        return response;
    }
}
