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

    private static readonly ThreadLocal<Random> Random = new(() => new Random());

    private static string[] LoadNames(string fileName)
    {
        var path = Path.Combine("Data", fileName);
        if (!File.Exists(path))
        {
            return Array.Empty<string>();
        }

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<string[]>(json) ?? Array.Empty<string>();
    }

    /// <summary>
    /// Generate a random fantasy name
    /// </summary>
    /// <returns>A random fantasy name</returns>
    public static string GenerateFantasyName()
    {
        return FantasyNames[Random.Value.Next(FantasyNames.Length)];
    }

    /// <summary>
    /// Generate a random adjective + fantasy name combination
    /// </summary>
    /// <returns>A random adjective + name combination</returns>
    public static string GenerateAdjectiveName()
    {
        var adjective = AdjectiveNames[Random.Value.Next(AdjectiveNames.Length)];
        var name = FantasyNames[Random.Value.Next(FantasyNames.Length)];
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
