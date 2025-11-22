using Microsoft.Extensions.Logging;
using Mystira.App.Domain.Models;
using Mystira.App.Infrastructure.Data.Repositories;
using Mystira.App.Infrastructure.Data.UnitOfWork;

namespace Mystira.App.Application.UseCases.Avatars;

/// <summary>
/// Use case for updating the avatar configuration file
/// </summary>
public class UpdateAvatarConfigurationUseCase
{
    private readonly IAvatarConfigurationFileRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateAvatarConfigurationUseCase> _logger;

    public UpdateAvatarConfigurationUseCase(
        IAvatarConfigurationFileRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<UpdateAvatarConfigurationUseCase> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<AvatarConfigurationFile> ExecuteAsync(Dictionary<string, List<string>> ageGroupAvatars)
    {
        if (ageGroupAvatars == null)
        {
            throw new ArgumentNullException(nameof(ageGroupAvatars));
        }

        var configFile = await _repository.GetAsync() ?? new AvatarConfigurationFile
        {
            Id = "avatar-configuration",
            CreatedAt = DateTime.UtcNow,
            Version = "1.0"
        };

        configFile.AgeGroupAvatars = ageGroupAvatars;
        configFile.UpdatedAt = DateTime.UtcNow;

        var result = await _repository.AddOrUpdateAsync(configFile);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Updated avatar configuration file with {Count} age groups", ageGroupAvatars.Count);
        return result;
    }
}

