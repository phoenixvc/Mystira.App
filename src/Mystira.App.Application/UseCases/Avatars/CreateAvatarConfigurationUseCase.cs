using Microsoft.Extensions.Logging;
using Mystira.App.Domain.Models;
using Mystira.App.Application.Ports.Data;

namespace Mystira.App.Application.UseCases.Avatars;

/// <summary>
/// Use case for creating a new avatar configuration file
/// </summary>
public class CreateAvatarConfigurationUseCase
{
    private readonly IAvatarConfigurationFileRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateAvatarConfigurationUseCase> _logger;

    public CreateAvatarConfigurationUseCase(
        IAvatarConfigurationFileRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<CreateAvatarConfigurationUseCase> logger)
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

        // Check if configuration already exists
        var existing = await _repository.GetAsync();
        if (existing != null)
        {
            throw new InvalidOperationException("Avatar configuration file already exists. Use update instead.");
        }

        var configFile = new AvatarConfigurationFile
        {
            Id = "avatar-configuration",
            AgeGroupAvatars = ageGroupAvatars,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Version = "1.0"
        };

        await _repository.AddOrUpdateAsync(configFile);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Created avatar configuration file with {Count} age groups", ageGroupAvatars.Count);
        return configFile;
    }
}

