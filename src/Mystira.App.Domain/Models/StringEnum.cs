using System.Text.Json;

namespace Mystira.App.Domain.Models;

public abstract class StringEnum<T> where T : StringEnum<T>
{
    private static readonly Lazy<Dictionary<string, T>> LazyValueMap = new(GetAll);

    internal static Dictionary<string, T> ValueMap => LazyValueMap.Value;

    public string Value { get; }

    protected StringEnum(string value)
    {
        Value = value;
    }

    private static Dictionary<string, T> GetAll()
    {
        var type = typeof(T);
        var fileName = $"Data/{type.Name}s.json";
        if (!File.Exists(fileName))
        {
            return new Dictionary<string, T>();
        }

        var json = File.ReadAllText(fileName);
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var values = JsonSerializer.Deserialize<List<T>>(json, options);
        return values?.ToDictionary(x => x.Value, x => x, StringComparer.OrdinalIgnoreCase) ?? new Dictionary<string, T>();
    }

    public static bool TryParse(string? value, out T? result)
    {
        if (value != null && ValueMap.TryGetValue(value, out var parsed))
        {
            result = parsed;
            return true;
        }

        result = default;
        return false;
    }

    public static T? Parse(string? value)
    {
        TryParse(value, out var result);
        return result;
    }

    public override string ToString() => Value;

    public override bool Equals(object? obj)
    {
        return obj is StringEnum<T> other && Value.Equals(other.Value, StringComparison.OrdinalIgnoreCase);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static bool operator ==(StringEnum<T>? left, StringEnum<T>? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(StringEnum<T>? left, StringEnum<T>? right)
    {
        return !Equals(left, right);
    }
}