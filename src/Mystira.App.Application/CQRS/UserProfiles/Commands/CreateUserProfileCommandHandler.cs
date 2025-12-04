using Microsoft.Extensions.Logging;
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
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Profile name is required");

        if (string.IsNullOrWhiteSpace(request.AgeGroup))
            throw new ArgumentException("Age group is required");

        if (!AgeGroupConstants.AllAgeGroups.Contains(request.AgeGroup))
            throw new ArgumentException($"Invalid age group: {request.AgeGroup}. Must be one of: {string.Join(", ", AgeGroupConstants.AllAgeGroups)}");

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
            AvatarMediaId = request.SelectedAvatarMediaId,
            SelectedAvatarMediaId = request.SelectedAvatarMediaId,
            HasCompletedOnboarding = request.HasCompletedOnboarding,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        profile.AgeGroupName = request.AgeGroup;

        // Update age group from date of birth if provided
        if (request.DateOfBirth.HasValue)
        {
            profile.UpdateAgeGroupFromBirthDate();
        }

        // Add to repository
        await _repository.AddAsync(profile);

        // Persist changes
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created user profile {ProfileId} with name {Name}", profile.Id, profile.Name);

        return profile;
    }
}
