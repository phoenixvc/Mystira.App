using Microsoft.Extensions.Logging;
using Mystira.App.Contracts.Requests.UserProfiles;
using Mystira.App.Domain.Models;
using Mystira.App.Infrastructure.Data.Repositories;
using Mystira.App.Infrastructure.Data.UnitOfWork;

namespace Mystira.App.Application.UseCases.UserProfiles;

/// <summary>
/// Use case for updating an existing user profile
/// </summary>
public class UpdateUserProfileUseCase
{
    private readonly IUserProfileRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateUserProfileUseCase> _logger;

    public UpdateUserProfileUseCase(
        IUserProfileRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<UpdateUserProfileUseCase> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<UserProfile?> ExecuteAsync(string id, UpdateUserProfileRequest request)
    {
        var profile = await _repository.GetByIdAsync(id);
        if (profile == null)
        {
            return null;
        }

        // Apply updates
        if (request.PreferredFantasyThemes != null)
        {
            // Validate fantasy themes
            var invalidThemes = request.PreferredFantasyThemes.Where(t => FantasyTheme.Parse(t) == null).ToList();
            if (invalidThemes.Any())
            {
                throw new ArgumentException($"Invalid fantasy themes: {string.Join(", ", invalidThemes)}");
            }

            profile.PreferredFantasyThemes = request.PreferredFantasyThemes.Select(t => FantasyTheme.Parse(t)!).ToList();
        }

        if (request.AgeGroup != null)
        {
            // Validate age group
            if (!AgeGroupConstants.AllAgeGroups.Contains(request.AgeGroup))
            {
                throw new ArgumentException($"Invalid age group: {request.AgeGroup}. Must be one of: {string.Join(", ", AgeGroupConstants.AllAgeGroups)}");
            }

            profile.AgeGroupName = request.AgeGroup;
        }

        if (request.DateOfBirth.HasValue)
        {
            profile.DateOfBirth = request.DateOfBirth;
            // Update age group automatically if date of birth is provided
            profile.UpdateAgeGroupFromBirthDate();
        }

        if (request.HasCompletedOnboarding.HasValue)
        {
            profile.HasCompletedOnboarding = request.HasCompletedOnboarding.Value;
        }

        if (request.IsGuest.HasValue)
        {
            profile.IsGuest = request.IsGuest.Value;
        }

        if (request.IsNpc.HasValue)
        {
            profile.IsNpc = request.IsNpc.Value;
        }

        if (request.AccountId != null)
        {
            profile.AccountId = request.AccountId;
        }

        if (request.Pronouns != null)
        {
            profile.Pronouns = request.Pronouns;
        }

        if (request.Bio != null)
        {
            profile.Bio = request.Bio;
        }

        profile.UpdatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(profile);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Updated user profile: {ProfileId} - {Name}", profile.Id, profile.Name);
        return profile;
    }
}

