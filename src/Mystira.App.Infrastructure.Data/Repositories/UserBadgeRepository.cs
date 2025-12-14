using Microsoft.EntityFrameworkCore;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;
using Mystira.App.Infrastructure.Data;

namespace Mystira.App.Infrastructure.Data.Repositories;

/// <summary>
/// Repository implementation for UserBadge entity
/// </summary>
public class UserBadgeRepository : Repository<UserBadge>, IUserBadgeRepository
{
    public UserBadgeRepository(DbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<UserBadge>> GetByUserProfileIdAsync(string userProfileId)
    {
        // UserBadge is configured as an owned collection of UserProfile (EarnedBadges)
        // Query via the owning entity instead of DbSet<UserBadge>
        return await _context.Set<UserProfile>()
            .Where(p => p.Id == userProfileId)
            .SelectMany(p => p.EarnedBadges)
            .OrderByDescending(b => b.EarnedAt)
            .ToListAsync();
    }

    public async Task<UserBadge?> GetByUserProfileIdAndBadgeConfigIdAsync(string userProfileId, string badgeConfigurationId)
    {
        return await _context.Set<UserProfile>()
            .Where(p => p.Id == userProfileId)
            .SelectMany(p => p.EarnedBadges)
            .FirstOrDefaultAsync(b => b.BadgeConfigurationId == badgeConfigurationId);
    }

    public async Task<IEnumerable<UserBadge>> GetByGameSessionIdAsync(string gameSessionId)
    {
        return await _context.Set<UserProfile>()
            .SelectMany(p => p.EarnedBadges)
            .Where(b => b.GameSessionId == gameSessionId)
            .OrderByDescending(b => b.EarnedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<UserBadge>> GetByScenarioIdAsync(string scenarioId)
    {
        return await _context.Set<UserProfile>()
            .SelectMany(p => p.EarnedBadges)
            .Where(b => b.ScenarioId == scenarioId)
            .OrderByDescending(b => b.EarnedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<UserBadge>> GetByUserProfileIdAndAxisAsync(string userProfileId, string axis)
    {
        return await _context.Set<UserProfile>()
            .Where(p => p.Id == userProfileId)
            .SelectMany(p => p.EarnedBadges)
            .Where(b => b.Axis == axis)
            .OrderByDescending(b => b.EarnedAt)
            .ToListAsync();
    }

    public override async Task<UserBadge> AddAsync(UserBadge entity)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));

        // Add the owned entity to the owner's collection
        var profile = await _context.Set<UserProfile>()
            .FirstOrDefaultAsync(p => p.Id == entity.UserProfileId);

        if (profile == null)
        {
            throw new InvalidOperationException($"UserProfile not found: {entity.UserProfileId}");
        }

        profile.EarnedBadges.Add(entity);
        // EF will track changes on the owned collection; UnitOfWork.SaveChangesAsync will persist
        return entity;
    }
}

