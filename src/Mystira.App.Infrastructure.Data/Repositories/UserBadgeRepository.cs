using Microsoft.EntityFrameworkCore;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;
using Mystira.App.Infrastructure.Data;

namespace Mystira.App.Infrastructure.Data.Repositories;

/// <summary>
/// Repository implementation for UserBadge entity.
/// Uses strategy pattern to handle InMemory vs Cosmos DB differences.
/// </summary>
public class UserBadgeRepository : Repository<UserBadge>, IUserBadgeRepository
{
    private readonly bool _isInMemory;

    public UserBadgeRepository(DbContext context) : base(context)
    {
        _isInMemory = context.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory";
    }

    public async Task<IEnumerable<UserBadge>> GetByUserProfileIdAsync(string userProfileId)
    {
        var badges = await QueryBadgesAsync(
            inMemoryQuery: () => _context.Set<UserBadge>()
                .AsNoTracking()
                .Where(b => b.UserProfileId == userProfileId),
            cosmosQuery: () => _context.Set<UserProfile>()
                .AsNoTracking()
                .Where(p => p.Id == userProfileId)
                .SelectMany(p => p.EarnedBadges)
        );
        return OrderByEarnedAtDescending(badges);
    }

    public async Task<UserBadge?> GetByUserProfileIdAndBadgeConfigIdAsync(string userProfileId, string badgeConfigurationId)
    {
        if (_isInMemory)
        {
            return await _context.Set<UserBadge>()
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.UserProfileId == userProfileId && b.BadgeConfigurationId == badgeConfigurationId);
        }

        return await _context.Set<UserProfile>()
            .AsNoTracking()
            .Where(p => p.Id == userProfileId)
            .SelectMany(p => p.EarnedBadges)
            .FirstOrDefaultAsync(b => b.BadgeConfigurationId == badgeConfigurationId);
    }

    public async Task<IEnumerable<UserBadge>> GetByGameSessionIdAsync(string gameSessionId)
    {
        var badges = await QueryBadgesAsync(
            inMemoryQuery: () => _context.Set<UserBadge>()
                .AsNoTracking()
                .Where(b => b.GameSessionId == gameSessionId),
            cosmosQuery: () => _context.Set<UserProfile>()
                .AsNoTracking()
                .SelectMany(p => p.EarnedBadges)
                .Where(b => b.GameSessionId == gameSessionId)
        );
        return OrderByEarnedAtDescending(badges);
    }

    public async Task<IEnumerable<UserBadge>> GetByScenarioIdAsync(string scenarioId)
    {
        var badges = await QueryBadgesAsync(
            inMemoryQuery: () => _context.Set<UserBadge>()
                .AsNoTracking()
                .Where(b => b.ScenarioId == scenarioId),
            cosmosQuery: () => _context.Set<UserProfile>()
                .AsNoTracking()
                .SelectMany(p => p.EarnedBadges)
                .Where(b => b.ScenarioId == scenarioId)
        );
        return OrderByEarnedAtDescending(badges);
    }

    public async Task<IEnumerable<UserBadge>> GetByUserProfileIdAndAxisAsync(string userProfileId, string axis)
    {
        var badges = await QueryBadgesAsync(
            inMemoryQuery: () => _context.Set<UserBadge>()
                .AsNoTracking()
                .Where(b => b.UserProfileId == userProfileId && b.Axis == axis),
            cosmosQuery: () => _context.Set<UserProfile>()
                .AsNoTracking()
                .Where(p => p.Id == userProfileId)
                .SelectMany(p => p.EarnedBadges)
                .Where(b => b.Axis == axis)
        );
        return OrderByEarnedAtDescending(badges);
    }

    public override async Task<UserBadge> AddAsync(UserBadge entity)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));

        if (_isInMemory)
        {
            await _context.Set<UserBadge>().AddAsync(entity);
            return entity;
        }

        // For Cosmos DB: Add the owned entity to the owner's collection
        var profile = await _context.Set<UserProfile>()
            .FirstOrDefaultAsync(p => p.Id == entity.UserProfileId);

        if (profile == null)
        {
            throw new InvalidOperationException($"UserProfile not found: {entity.UserProfileId}");
        }

        profile.EarnedBadges.Add(entity);
        return entity;
    }

    /// <summary>
    /// Executes the appropriate query based on database provider
    /// </summary>
    private async Task<List<UserBadge>> QueryBadgesAsync(
        Func<IQueryable<UserBadge>> inMemoryQuery,
        Func<IQueryable<UserBadge>> cosmosQuery)
    {
        var query = _isInMemory ? inMemoryQuery() : cosmosQuery();
        return await query.ToListAsync();
    }

    /// <summary>
    /// Orders badges by earned date descending
    /// </summary>
    private static List<UserBadge> OrderByEarnedAtDescending(List<UserBadge> badges)
    {
        return badges.OrderByDescending(b => b.EarnedAt).ToList();
    }
}
