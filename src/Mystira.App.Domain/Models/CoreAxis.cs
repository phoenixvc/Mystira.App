namespace Mystira.App.Domain.Models;

/// <summary>
/// Represents a core axis value used in scenarios for character development.
/// </summary>
public class CoreAxis
{
    public string Value { get; set; }

    public CoreAxis(string value)
    {
        Value = value;
    }

    public override string ToString() => Value;

    public override bool Equals(object? obj)
    {
        return obj is CoreAxis other && Value.Equals(other.Value, StringComparison.OrdinalIgnoreCase);
    }

    public override int GetHashCode()
    {
        return StringComparer.OrdinalIgnoreCase.GetHashCode(Value);
    }

    public static bool operator ==(CoreAxis? left, CoreAxis? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(CoreAxis? left, CoreAxis? right)
    {
        return !Equals(left, right);
    }

    public static CoreAxis? Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        return new CoreAxis(value);
    }
}
