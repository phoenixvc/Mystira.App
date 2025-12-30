using Mystira.Domain.Models;

namespace Mystira.App.Application.Parsers;

/// <summary>
/// Parser for converting character dictionary data to ScenarioCharacter domain object
/// </summary>
public static class CharacterParser
{
    public static ScenarioCharacter Parse(IDictionary<object, object> characterDict)
    {
        if (!characterDict.TryGetValue("id", out var idObj) || idObj == null)
        {
            throw new ArgumentException("Required field 'id' is missing or null in character data");
        }

        if (!characterDict.TryGetValue("name", out var nameObj) || nameObj == null)
        {
            throw new ArgumentException("Required field 'name' is missing or null in character data");
        }

        var character = new ScenarioCharacter
        {
            Id = idObj.ToString() ?? string.Empty,
            Name = nameObj.ToString() ?? string.Empty
        };

        if (characterDict.TryGetValue("image", out var imageObj))
        {
            character.Image = imageObj?.ToString();
        }

        if (characterDict.TryGetValue("audio", out var audioObj))
        {
            character.Audio = audioObj?.ToString();
        }

        if (!characterDict.TryGetValue("metadata", out var metadataObj) || metadataObj is not IDictionary<object, object> metadataDict)
        {
            throw new ArgumentException("Required field 'metadata' is missing or invalid in character data");
        }

        character.Metadata = CharacterMetadataParser.Parse(metadataDict);

        return character;
    }
}

