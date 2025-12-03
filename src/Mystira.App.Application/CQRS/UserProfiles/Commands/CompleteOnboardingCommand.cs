namespace Mystira.App.Application.CQRS.UserProfiles.Commands;

/// <summary>
/// Command to mark onboarding as complete for a user profile
/// </summary>
public record CompleteOnboardingCommand(string ProfileId) : ICommand<bool>;
