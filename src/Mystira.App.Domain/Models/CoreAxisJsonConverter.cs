using System.Text.Json;
using System.Text.Json.Serialization;

namespace Mystira.App.Domain.Models;

/// <summary>
/// Custom JSON converter for CoreAxis to serialize/deserialize as a string value.
/// </summary>
public class CoreAxisJsonConverter : JsonConverter<CoreAxis>
{
    public override CoreAxis? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var stringValue = reader.GetString();
            return stringValue != null ? new CoreAxis(stringValue) : null;
        }
        
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        throw new JsonException($"Unexpected token {reader.TokenType} when parsing CoreAxis");
    }

    public override void Write(Utf8JsonWriter writer, CoreAxis? value, JsonSerializerOptions options)
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
