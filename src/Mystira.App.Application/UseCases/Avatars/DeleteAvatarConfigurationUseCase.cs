using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Application.Ports.Data;

namespace Mystira.App.Application.UseCases.Avatars;

/// <summary>
/// Use case for deleting the avatar configuration file
/// </summary>
public class DeleteAvatarConfigurationUseCase
{
    private readonly IAvatarConfigurationFileRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteAvatarConfigurationUseCase> _logger;

    public DeleteAvatarConfigurationUseCase(
        IAvatarConfigurationFileRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<DeleteAvatarConfigurationUseCase> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<bool> ExecuteAsync()
    {
        var configFile = await _repository.GetAsync();
        if (configFile == null)
        {
            _logger.LogWarning("Avatar configuration file not found for deletion");
            return false;
        }

        await _repository.DeleteAsync();
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Deleted avatar configuration file");
        return true;
    }
}

