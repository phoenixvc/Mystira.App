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
        var isInMemory = _context.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory";
        if (isInMemory)
        {
            var list = await _context.Set<UserBadge>()
                .AsNoTracking()
                .Where(b => b.UserProfileId == userProfileId)
                .ToListAsync();
            return list.OrderByDescending(b => b.EarnedAt).ToList();
        }
        else
        {
            // UserBadge is configured as an owned collection of UserProfile (EarnedBadges)
            var badges = await _context.Set<UserProfile>()
                .AsNoTracking()
                .Where(p => p.Id == userProfileId)
                .SelectMany(p => p.EarnedBadges)
                .ToListAsync();
            return badges.OrderByDescending(b => b.EarnedAt).ToList();
        }
    }

    public async Task<UserBadge?> GetByUserProfileIdAndBadgeConfigIdAsync(string userProfileId, string badgeConfigurationId)
    {
        var isInMemory = _context.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory";
        if (isInMemory)
        {
            return await _context.Set<UserBadge>()
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.UserProfileId == userProfileId && b.BadgeConfigurationId == badgeConfigurationId);
        }
        else
        {
            return await _context.Set<UserProfile>()
                .AsNoTracking()
                .Where(p => p.Id == userProfileId)
                .SelectMany(p => p.EarnedBadges)
                .FirstOrDefaultAsync(b => b.BadgeConfigurationId == badgeConfigurationId);
        }
    }

    public async Task<IEnumerable<UserBadge>> GetByGameSessionIdAsync(string gameSessionId)
    {
        var isInMemory = _context.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory";
        if (isInMemory)
        {
            var list = await _context.Set<UserBadge>()
                .AsNoTracking()
                .Where(b => b.GameSessionId == gameSessionId)
                .ToListAsync();
            return list.OrderByDescending(b => b.EarnedAt).ToList();
        }
        else
        {
            var badges = await _context.Set<UserProfile>()
                .AsNoTracking()
                .SelectMany(p => p.EarnedBadges)
                .Where(b => b.GameSessionId == gameSessionId)
                .ToListAsync();
            return badges.OrderByDescending(b => b.EarnedAt).ToList();
        }
    }

    public async Task<IEnumerable<UserBadge>> GetByScenarioIdAsync(string scenarioId)
    {
        var isInMemory = _context.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory";
        if (isInMemory)
        {
            var list = await _context.Set<UserBadge>()
                .AsNoTracking()
                .Where(b => b.ScenarioId == scenarioId)
                .ToListAsync();
            return list.OrderByDescending(b => b.EarnedAt).ToList();
        }
        else
        {
            var badges = await _context.Set<UserProfile>()
                .AsNoTracking()
                .SelectMany(p => p.EarnedBadges)
                .Where(b => b.ScenarioId == scenarioId)
                .ToListAsync();
            return badges.OrderByDescending(b => b.EarnedAt).ToList();
        }
    }

    public async Task<IEnumerable<UserBadge>> GetByUserProfileIdAndAxisAsync(string userProfileId, string axis)
    {
        var isInMemory = _context.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory";
        if (isInMemory)
        {
            var list = await _context.Set<UserBadge>()
                .AsNoTracking()
                .Where(b => b.UserProfileId == userProfileId && b.Axis == axis)
                .ToListAsync();
            return list.OrderByDescending(b => b.EarnedAt).ToList();
        }
        else
        {
            var badges = await _context.Set<UserProfile>()
                .AsNoTracking()
                .Where(p => p.Id == userProfileId)
                .SelectMany(p => p.EarnedBadges)
                .Where(b => b.Axis == axis)
                .ToListAsync();
            return badges.OrderByDescending(b => b.EarnedAt).ToList();
        }
    }

    public override async Task<UserBadge> AddAsync(UserBadge entity)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));
        var isInMemory = _context.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory";
        if (isInMemory)
        {
            await _context.Set<UserBadge>().AddAsync(entity);
            return entity;
        }
        else
        {
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
}

