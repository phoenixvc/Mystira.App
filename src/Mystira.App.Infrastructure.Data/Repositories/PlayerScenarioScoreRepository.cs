using Microsoft.EntityFrameworkCore;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;

namespace Mystira.App.Infrastructure.Data.Repositories;

public class PlayerScenarioScoreRepository : Repository<PlayerScenarioScore>, IPlayerScenarioScoreRepository
{
    private readonly MystiraAppDbContext _context;

    public PlayerScenarioScoreRepository(MystiraAppDbContext context)
        : base(context)
    {
        _context = context;
    }

    public async Task<PlayerScenarioScore?> GetByProfileAndScenarioAsync(string profileId, string scenarioId)
    {
        return await _context.PlayerScenarioScores
            .FirstOrDefaultAsync(x => x.ProfileId == profileId && x.ScenarioId == scenarioId);
    }

    public async Task<IEnumerable<PlayerScenarioScore>> GetByProfileIdAsync(string profileId)
    {
        return await _context.PlayerScenarioScores
            .Where(x => x.ProfileId == profileId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
    }

    public async Task<bool> IsScenarioScoredAsync(string profileId, string scenarioId)
    {
        return await _context.PlayerScenarioScores
            .AnyAsync(x => x.ProfileId == profileId && x.ScenarioId == scenarioId);
    }
}
