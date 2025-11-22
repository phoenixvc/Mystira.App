using Mystira.App.Domain.Models;

namespace Mystira.App.Api.Models.Parsers;

/// <summary>
/// Parser for converting character metadata dictionary data to ScenarioCharacterMetadata domain object
/// </summary>
public static class CharacterMetadataParser
{
    public static ScenarioCharacterMetadata Parse(IDictionary<object, object> metadataDict)
    {
        metadataDict.TryGetValue("role", out var roleObj);
        metadataDict.TryGetValue("archetype", out var archetypeObj);
        metadataDict.TryGetValue("traits", out var traitsObj);

        var metadata = new ScenarioCharacterMetadata
        {
            Role = ToStringList(roleObj),
            Archetype = ToStringList(archetypeObj).Select(a => Archetype.Parse(a)!).ToList(),
            Traits = ToStringList(traitsObj)
        };

        if (!metadataDict.TryGetValue("species", out var speciesObj) || speciesObj == null)
        {
            throw new ArgumentException("Required field 'species' is missing or null in character metadata");
        }

        metadata.Species = speciesObj.ToString() ?? string.Empty;

        if (!metadataDict.TryGetValue("age", out var ageObj) || ageObj == null || !int.TryParse(ageObj.ToString(), out var age))
        {
            throw new ArgumentException("Required field 'age' is missing or invalid in character metadata");
        }

        metadata.Age = age;

        if (!metadataDict.TryGetValue("backstory", out var backstoryObj) || backstoryObj == null)
        {
            throw new ArgumentException("Required field 'backstory' is missing or null in character metadata");
        }

        metadata.Backstory = backstoryObj.ToString() ?? string.Empty;

        return metadata;
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

