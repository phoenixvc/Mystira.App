using System.Text.Json;

namespace Mystira.App.Domain.Models;

/// <summary>
/// Utility class for generating random names for guest profiles
/// </summary>
public static class RandomNameGenerator
{
    private static readonly Lazy<string[]> FantasyNamesLazy = new(() => LoadNames("FantasyNames.json"));
    private static readonly Lazy<string[]> AdjectiveNamesLazy = new(() => LoadNames("AdjectiveNames.json"));

    internal static string[] FantasyNames => FantasyNamesLazy.Value;
    internal static string[] AdjectiveNames => AdjectiveNamesLazy.Value;

    private static readonly ThreadLocal<Random> ThreadLocalRandom = new(() => new Random());

    /// <summary>
    /// Gets the thread-local Random instance, ensuring it is never null.
    /// </summary>
    private static Random RandomInstance => ThreadLocalRandom.Value ?? new Random();

    private static string[] LoadNames(string fileName)
    {
        var assembly = typeof(RandomNameGenerator).Assembly; // ensure we read from Domain assembly
        var resourceName = $"Mystira.App.Domain.Data.{fileName}";

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            return Array.Empty<string>();
        }

        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind == JsonValueKind.Array)
            {
                var list = new List<string>();
                foreach (var el in doc.RootElement.EnumerateArray())
                {
                    if (el.ValueKind == JsonValueKind.Object)
                    {
                        string? value = null;
                        foreach (var prop in el.EnumerateObject())
                        {
                            if (string.Equals(prop.Name, "value", StringComparison.OrdinalIgnoreCase)
                                && prop.Value.ValueKind == JsonValueKind.String)
                            {
                                value = prop.Value.GetString();
                                break;
                            }
                        }
                        if (!string.IsNullOrWhiteSpace(value))
                        {
                            list.Add(value);
                        }
                    }
                    else if (el.ValueKind == JsonValueKind.String)
                    {
                        // Backward compatibility if any string entries remain
                        list.Add(el.GetString()!);
                    }
                }

                return list.ToArray();
            }
        }
        catch
        {
            // ignore and return empty below
        }

        return Array.Empty<string>();
    }

    /// <summary>
    /// Generate a random fantasy name
    /// </summary>
    /// <returns>A random fantasy name</returns>
    public static string GenerateFantasyName()
    {
        return FantasyNames[RandomInstance.Next(FantasyNames.Length)];
    }

    /// <summary>
    /// Generate a random adjective + fantasy name combination
    /// </summary>
    /// <returns>A random adjective + name combination</returns>
    public static string GenerateAdjectiveName()
    {
        var adjective = AdjectiveNames[RandomInstance.Next(AdjectiveNames.Length)];
        var name = FantasyNames[RandomInstance.Next(FantasyNames.Length)];
        return $"{adjective} {name}";
    }

    /// <summary>
    /// Generate a random guest name (can be simple or adjective-based)
    /// </summary>
    /// <param name="useAdjective">Whether to include an adjective</param>
    /// <returns>A random guest name</returns>
    public static string GenerateGuestName(bool useAdjective = false)
    {
        return useAdjective ? GenerateAdjectiveName() : GenerateFantasyName();
    }

    /// <summary>
    /// Generate multiple unique guest names
    /// </summary>
    /// <param name="count">Number of names to generate</param>
    /// <param name="useAdjective">Whether to include adjectives</param>
    /// <returns>List of unique guest names</returns>
    public static List<string> GenerateUniqueGuestNames(int count, bool useAdjective = false)
    {
        var maxUniqueNames = useAdjective ? AdjectiveNames.Length * FantasyNames.Length : FantasyNames.Length;
        if (count > maxUniqueNames)
        {
            throw new ArgumentException($"Cannot generate more unique names than the number of possibilities ({maxUniqueNames}).", nameof(count));
        }

        var names = new HashSet<string>();
        var attempts = 0;
        var maxAttempts = count * 10; // Prevent infinite loops

        while (names.Count < count && attempts < maxAttempts)
        {
            names.Add(GenerateGuestName(useAdjective));
            attempts++;
        }

        return names.ToList();
    }
}
