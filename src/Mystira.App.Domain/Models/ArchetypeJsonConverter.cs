using System.Text.Json;
using System.Text.Json.Serialization;

namespace Mystira.App.Domain.Models;

/// <summary>
/// Custom JSON converter for Archetype to serialize/deserialize as a string value.
/// </summary>
public class ArchetypeJsonConverter : JsonConverter<Archetype>
{
    public override Archetype? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var stringValue = reader.GetString();
            return stringValue != null ? new Archetype(stringValue) : null;
        }
        
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        throw new JsonException($"Unexpected token {reader.TokenType} when parsing Archetype");
    }

    public override void Write(Utf8JsonWriter writer, Archetype? value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue();
        }
        else
        {
            writer.WriteStringValue(value.Value);
        }
    }
}
