using Mystira.Domain.Models;

namespace Mystira.App.Application.CQRS.UserProfiles.Queries;

/// <summary>
/// Query to retrieve a user profile by ID
/// </summary>
public record GetUserProfileQuery(string ProfileId) : IQuery<UserProfile?>;
