using Microsoft.Extensions.Logging;
using Mystira.App.Application.Interfaces;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.UserProfiles.Commands;

/// <summary>
/// Handler for CreateUserProfileCommand
/// Creates a new user profile with the specified details
/// </summary>
public class CreateUserProfileCommandHandler : ICommandHandler<CreateUserProfileCommand, UserProfile>
{
    private readonly IUserProfileRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateUserProfileCommandHandler> _logger;

    public CreateUserProfileCommandHandler(
        IUserProfileRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<CreateUserProfileCommandHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<UserProfile> Handle(
        CreateUserProfileCommand command,
        CancellationToken cancellationToken)
    {
        var request = command.Request;

        // Validate request
        if (string.IsNullOrEmpty(request.Name))
            throw new ArgumentException("Profile name is required");

        // Check if profile name already exists
        var exists = await _repository.ExistsByNameAsync(request.Name);
        if (exists)
            throw new ArgumentException($"Profile name '{request.Name}' already exists");

        // Create profile
        var profile = new UserProfile
        {
            Id = Guid.NewGuid().ToString(),
            Name = request.Name,
            DateOfBirth = request.DateOfBirth,
            IsGuest = request.IsGuest,
            IsNpc = request.IsNpc,
            AccountId = request.AccountId,
            Pronouns = request.Pronouns,
            Bio = request.Bio,
            PreferredFantasyThemes = request.PreferredFantasyThemes?
                .Select(t => new FantasyTheme(t))
                .ToList() ?? new List<FantasyTheme>(),
            SelectedAvatarMediaId = request.SelectedAvatarMediaId,
            HasCompletedOnboarding = request.HasCompletedOnboarding,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Update age group from date of birth if provided
        if (request.DateOfBirth.HasValue)
        {
            profile.UpdateAgeGroupFromBirthDate();
        }

        // Add to repository
        await _repository.AddAsync(profile);

        // Persist changes
        await _unitOfWork.CommitAsync(cancellationToken);

        _logger.LogInformation("Created user profile {ProfileId} with name {Name}", profile.Id, profile.Name);

        return profile;
    }
}
