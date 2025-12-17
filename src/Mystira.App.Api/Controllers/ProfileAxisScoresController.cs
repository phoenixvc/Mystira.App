using Microsoft.AspNetCore.Mvc;
using Mystira.App.Application.Ports.Data;

namespace Mystira.App.Api.Controllers;

[ApiController]
[Route("api/profiles/{profileId}/axis-scores")]
public class ProfileAxisScoresController : ControllerBase
{
    private readonly IPlayerScenarioScoreRepository _scoreRepository;

    public ProfileAxisScoresController(IPlayerScenarioScoreRepository scoreRepository)
    {
        _scoreRepository = scoreRepository;
    }

    public class AxisScoreItem
    {
        public string ScenarioId { get; set; } = string.Empty;
        public string GameSessionId { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public Dictionary<string, float> AxisScores { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }

    public class AxisScoresResponse
    {
        public string ProfileId { get; set; } = string.Empty;
        public List<AxisScoreItem> Items { get; set; } = new();
    }

    [HttpGet]
    public async Task<ActionResult<AxisScoresResponse>> Get(string profileId)
    {
        var scores = await _scoreRepository.GetByProfileIdAsync(profileId);
        var items = scores.Select(s => new AxisScoreItem
        {
            ScenarioId = s.ScenarioId,
            GameSessionId = s.GameSessionId,
            CreatedAt = s.CreatedAt,
            AxisScores = new Dictionary<string, float>(s.AxisScores, StringComparer.OrdinalIgnoreCase)
        }).ToList();

        var response = new AxisScoresResponse
        {
            ProfileId = profileId,
            Items = items
        };

        return Ok(response);
    }
}
