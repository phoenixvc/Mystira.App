using Mystira.App.Application.Interfaces;
using Mystira.App.Contracts.Requests.UserProfiles;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.UserProfiles.Commands;

/// <summary>
/// Command to update an existing user profile
/// </summary>
public record UpdateUserProfileCommand(string ProfileId, UpdateUserProfileRequest Request) : ICommand<UserProfile?>;
