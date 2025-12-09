using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.UserProfiles.Queries;

/// <summary>
/// Query to retrieve all user profiles for a specific account
/// </summary>
public record GetProfilesByAccountQuery(string AccountId) : IQuery<List<UserProfile>>;
