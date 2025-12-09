using System.Text.Json.Serialization;

namespace Mystira.App.Domain.Models;

/// <summary>
/// Represents an archetype value used in scenarios and characters.
/// </summary>
[JsonConverter(typeof(ArchetypeJsonConverter))]
public class Archetype
{
    public string Value { get; set; }

    public Archetype(string value)
    {
        Value = value;
    }

    public override string ToString() => Value;

    public override bool Equals(object? obj)
    {
        return obj is Archetype other && Value.Equals(other.Value, StringComparison.OrdinalIgnoreCase);
    }

    public override int GetHashCode()
    {
        return StringComparer.OrdinalIgnoreCase.GetHashCode(Value);
    }

    public static bool operator ==(Archetype? left, Archetype? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(Archetype? left, Archetype? right)
    {
        return !Equals(left, right);
    }

    public static Archetype? Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return new Archetype(value);
    }
}
