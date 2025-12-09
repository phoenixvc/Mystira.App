using System.Text.Json;
using System.Text.Json.Serialization;

namespace Mystira.App.Domain.Models;

/// <summary>
/// Custom JSON converter for EchoType to serialize/deserialize as a string value.
/// </summary>
public class EchoTypeJsonConverter : JsonConverter<EchoType>
{
    public override EchoType? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var stringValue = reader.GetString();
            return stringValue != null ? new EchoType(stringValue) : null;
        }

        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        throw new JsonException($"Unexpected token {reader.TokenType} when parsing EchoType");
    }

    public override void Write(Utf8JsonWriter writer, EchoType? value, JsonSerializerOptions options)
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
