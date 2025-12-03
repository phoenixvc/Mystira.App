using System.Text.Json;

namespace Mystira.App.Domain.Models;

public abstract class StringEnum<T> where T : StringEnum<T>
{
    private static readonly Lazy<Dictionary<string, T>> LazyValueMap = new(GetAll);

    public static IReadOnlyDictionary<string, T> ValueMap => LazyValueMap.Value;

    public string Value { get; set; }

    protected StringEnum(string value)
    {
        Value = value;
    }

    private static Dictionary<string, T> GetAll()
    {
        var type = typeof(T);
        var assembly = type.Assembly; // ensure we read from the assembly that defines T
        var resourceName = $"Mystira.App.Domain.Data.{type.Name}s.json";

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            return new Dictionary<string, T>(StringComparer.OrdinalIgnoreCase);
        }

        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        // New canonical schema: array of objects with { "value": "..." }
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind == JsonValueKind.Array)
            {
                var list = new List<T>();
                foreach (var el in doc.RootElement.EnumerateArray())
                {
                    string? value = null;
                    if (el.ValueKind == JsonValueKind.Object)
                    {
                        foreach (var prop in el.EnumerateObject())
                        {
                            if (string.Equals(prop.Name, "value", StringComparison.OrdinalIgnoreCase)
                                && prop.Value.ValueKind == JsonValueKind.String)
                            {
                                value = prop.Value.GetString();
                                break;
                            }
                        }
                    }
                    else if (el.ValueKind == JsonValueKind.String)
                    {
                        // Backward compatibility (if any old files still exist)
                        value = el.GetString();
                    }

                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        // Construct T via (string) constructor
                        var instance = (T)Activator.CreateInstance(typeof(T), value)!;
                        list.Add(instance);
                    }
                }

                return list.ToDictionary(x => x.Value, x => x, StringComparer.OrdinalIgnoreCase);
            }
        }
        catch
        {
            // If parsing fails, fall through to empty
        }

        return new Dictionary<string, T>(StringComparer.OrdinalIgnoreCase);
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
        return StringComparer.OrdinalIgnoreCase.GetHashCode(Value);
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
