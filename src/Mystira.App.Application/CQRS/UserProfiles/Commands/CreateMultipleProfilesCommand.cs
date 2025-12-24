using Mystira.Contracts.App.Requests.UserProfiles;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.UserProfiles.Commands;

/// <summary>
/// Command to create multiple user profiles in a batch operation.
/// Used during onboarding when creating profiles for family members.
/// </summary>
public record CreateMultipleProfilesCommand(
    CreateMultipleProfilesRequest Request
) : ICommand<List<UserProfile>>;
