using System.Text.Json;
using System.Text.Json.Serialization;

namespace Mystira.App.Domain.Models;

/// <summary>
/// JSON converter for StringEnum types
/// </summary>
public class StringEnumJsonConverter<T> : JsonConverter<T> where T : StringEnum<T>
{
    public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return default;
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            var value = reader.GetString();
            return StringEnum<T>.Parse(value);
        }

        throw new JsonException($"Cannot convert token type {reader.TokenType} to {typeof(T).Name}");
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStringValue(value.Value);
    }
}
