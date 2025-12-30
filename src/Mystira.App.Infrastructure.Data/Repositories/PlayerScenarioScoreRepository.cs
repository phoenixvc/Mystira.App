using Microsoft.EntityFrameworkCore;
using Mystira.App.Application.Ports.Data;
using Mystira.Domain.Models;

namespace Mystira.App.Infrastructure.Data.Repositories;

public class PlayerScenarioScoreRepository : Repository<PlayerScenarioScore>, IPlayerScenarioScoreRepository
{
    public PlayerScenarioScoreRepository(MystiraAppDbContext context)
        : base(context)
    {
    }

    public async Task<PlayerScenarioScore?> GetByProfileAndScenarioAsync(string profileId, string scenarioId)
    {
        return await ((MystiraAppDbContext)_context).PlayerScenarioScores
            .FirstOrDefaultAsync(x => x.ProfileId == profileId && x.ScenarioId == scenarioId);
    }

    public async Task<IEnumerable<PlayerScenarioScore>> GetByProfileIdAsync(string profileId)
    {
        return await ((MystiraAppDbContext)_context).PlayerScenarioScores
            .Where(x => x.ProfileId == profileId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
    }

    public async Task<bool> IsScenarioScoredAsync(string profileId, string scenarioId)
    {
        return await ((MystiraAppDbContext)_context).PlayerScenarioScores
            .AnyAsync(x => x.ProfileId == profileId && x.ScenarioId == scenarioId);
    }
}
