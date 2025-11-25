using System.Collections;
using System.Data;
using Mystira.App.Contracts.Requests.Scenarios;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.Parsers;

/// <summary>
/// Parser for converting scenario dictionary data to CreateScenarioRequest
/// </summary>
public static class ScenarioParser
{
    public static CreateScenarioRequest Create(Dictionary<object, object> scenarioData)
    {
        ValidateScenarioData(scenarioData);

        var coreAxesRaw = scenarioData.GetValueOrDefault("core_axes")
                          ?? scenarioData.GetValueOrDefault("compass_axes", new List<object>());
        var coreAxesList = ToStringList(coreAxesRaw);

        var charactersObj = scenarioData["characters"] as IList<object>
            ?? throw new DataException("Scenario does not contain characters.");
        var scenes = (List<object>)scenarioData.GetValueOrDefault("scenes", new List<object>());

        if (scenes.Count == 0)
        {
            throw new Exception("Scenario does not contain any scenes.");
        }

        // Convert to CreateScenarioRequest format
        var createRequest = new CreateScenarioRequest
        {
            Title = (string)scenarioData["title"]!,
            Description = (string)scenarioData["description"]!,
            Tags = ((List<object>)scenarioData["tags"]!).Select(o => (string)o).ToList(),
            Difficulty = Enum.Parse<DifficultyLevel>((string)scenarioData["difficulty"]!, true),
            SessionLength = Enum.Parse<SessionLength>((string)scenarioData["session_length"]!, true),
            Archetypes = ((List<object>)scenarioData["archetypes"]!).Select(o => (string)o).ToList(),
            AgeGroup = scenarioData["age_group"]?.ToString() ?? string.Empty,
            MinimumAge = Convert.ToInt32(scenarioData["minimum_age"]!),
            CoreAxes = coreAxesList,
            Characters = charactersObj.Select(o => CharacterParser.Parse((Dictionary<object, object>)o)).ToList(),
            Scenes = scenes.Select(o => SceneParser.Parse((Dictionary<object, object>)o)).ToList()
        };

        createRequest.CompassAxes = createRequest.CoreAxes;

        return createRequest;
    }

    private static void ValidateScenarioData(Dictionary<object, object> scenarioData)
    {
        if (!scenarioData.TryGetValue("title", out var title) || title == null)
        {
            throw new DataException("Scenario does not contain a title.");
        }

        if (!scenarioData.TryGetValue("description", out var description) || description == null)
        {
            throw new DataException("Scenario does not contain a description.");
        }

        if (!scenarioData.TryGetValue("tags", out var tags) || tags == null)
        {
            throw new DataException("Scenario does not contain tags.");
        }

        if (!scenarioData.TryGetValue("difficulty", out var d) || d == null)
        {
            throw new DataException("Scenario does not contain a difficulty.");
        }

        if (!Enum.TryParse<DifficultyLevel>((string)d, true, out _))
        {
            throw new DataException("Scenario does not contain a valid difficulty level.");
        }

        if (!scenarioData.TryGetValue("session_length", out var s) || s == null)
        {
            throw new DataException("Scenario does not contain session_length.");
        }

        if (!Enum.TryParse<SessionLength>((string)s, true, out _))
        {
            throw new DataException("Scenario does not contain a valid session_length.");
        }

        if (!scenarioData.TryGetValue("archetypes", out var archetypes) || archetypes == null)
        {
            throw new DataException("Scenario does not contain archetypes.");
        }

        if (!scenarioData.TryGetValue("age_group", out var ageGroup) || ageGroup == null)
        {
            throw new DataException("Scenario does not contain age_group.");
        }

        if (!scenarioData.TryGetValue("minimum_age", out var minimumAge) || minimumAge == null)
        {
            throw new DataException("Scenario does not contain minimum_age.");
        }

        if (!scenarioData.TryGetValue("characters", out var charactersObj) || charactersObj is not IList<object>)
        {
            throw new DataException("Scenario does not contain characters.");
        }
    }

    private static List<string> ToStringList(object? value)
    {
        if (value is string single && !string.IsNullOrWhiteSpace(single))
        {
            return new List<string> { single };
        }

        if (value is IEnumerable enumerable)
        {
            var results = new List<string>();
            foreach (var item in enumerable)
            {
                var str = item?.ToString();
                if (!string.IsNullOrWhiteSpace(str))
                {
                    results.Add(str!);
                }
            }
            return results;
        }

        return new List<string>();
    }
}

