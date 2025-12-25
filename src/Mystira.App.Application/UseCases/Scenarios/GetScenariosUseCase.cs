using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.Contracts.App.Requests.Scenarios;
using Mystira.Contracts.App.Responses.Scenarios;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.UseCases.Scenarios;

/// <summary>
/// Use case for retrieving scenarios with filtering and pagination
/// </summary>
public class GetScenariosUseCase
{
    private readonly IScenarioRepository _repository;
    private readonly ILogger<GetScenariosUseCase> _logger;

    public GetScenariosUseCase(
        IScenarioRepository repository,
        ILogger<GetScenariosUseCase> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<ScenarioListResponse> ExecuteAsync(ScenarioQueryRequest request)
    {
        var query = _repository.GetQueryable();

        // Apply filters
        if (request.Difficulty.HasValue)
        {
            var difficulty = (DifficultyLevel)(int)request.Difficulty.Value;
            query = query.Where(s => s.Difficulty == difficulty);
        }

        if (request.SessionLength.HasValue)
        {
            var sessionLength = (SessionLength)(int)request.SessionLength.Value;
            query = query.Where(s => s.SessionLength == sessionLength);
        }

        if (request.MinimumAge.HasValue)
        {
            query = query.Where(s => s.MinimumAge <= request.MinimumAge.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.AgeGroup))
        {
            query = query.Where(s => s.AgeGroup == request.AgeGroup);
        }

        if (request.Tags != null && request.Tags.Count > 0)
        {
            query = query.Where(s => request.Tags.Any(tag => s.Tags.Contains(tag)));
        }

        if (request.Archetypes != null && request.Archetypes.Count > 0)
        {
            var archetypeValues = request.Archetypes.Where(a => Archetype.Parse(a)?.Value != null).Select(a => Archetype.Parse(a)!.Value).ToList();
            query = query.Where(s => s.Archetypes.Any(a => archetypeValues.Contains(a.Value)));
        }

        if (request.CoreAxes != null && request.CoreAxes.Count > 0)
        {
            var axisValues = request.CoreAxes.Where(a => CoreAxis.Parse(a)?.Value != null).Select(a => CoreAxis.Parse(a)!.Value).ToList();
            query = query.Where(s => s.CoreAxes.Any(a => axisValues.Contains(a.Value)));
        }

        // Get total count before pagination
        var totalCount = await query.CountAsync();

        // Apply pagination
        var scenarios = await query
            .OrderByDescending(s => s.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(s => new ScenarioSummary
            {
                Id = s.Id,
                Title = s.Title,
                Description = s.Description,
                Tags = s.Tags,
                Difficulty = s.Difficulty.ToString(),
                SessionLength = s.SessionLength.ToString(),
                Archetypes = s.Archetypes.Select(a => a.Value).ToList(),
                MinimumAge = s.MinimumAge,
                AgeGroup = s.AgeGroup,
                CoreAxes = s.CoreAxes.Select(a => a.Value).ToList(),
                CreatedAt = s.CreatedAt,
                MusicPalette = s.MusicPalette?.DefaultProfile.ToString()
            })
            .ToListAsync();

        return new ScenarioListResponse
        {
            Scenarios = scenarios,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize,
            HasNextPage = (request.Page * request.PageSize) < totalCount
        };
    }
}

