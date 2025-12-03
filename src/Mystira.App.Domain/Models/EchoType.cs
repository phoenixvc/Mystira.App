using System.Text.Json.Serialization;

namespace Mystira.App.Domain.Models;

/// <summary>
/// Represents an echo type value used in scenarios for game events and progression.
/// </summary>
[JsonConverter(typeof(EchoTypeJsonConverter))]
public class EchoType
{
    public string Value { get; set; }

    public EchoType(string value)
    {
        Value = value;
    }

    public override string ToString() => Value;

    public override bool Equals(object? obj)
    {
        return obj is EchoType other && Value.Equals(other.Value, StringComparison.OrdinalIgnoreCase);
    }

    public override int GetHashCode()
    {
        return StringComparer.OrdinalIgnoreCase.GetHashCode(Value);
    }

    public static bool operator ==(EchoType? left, EchoType? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(EchoType? left, EchoType? right)
    {
        return !Equals(left, right);
    }

    public static EchoType? Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        return new EchoType(value);
    }
}
