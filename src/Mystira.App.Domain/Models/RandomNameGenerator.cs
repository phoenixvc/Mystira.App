namespace Mystira.App.Domain.Models;

/// <summary>
/// Utility class for generating random names for guest profiles
/// </summary>
public static class RandomNameGenerator
{
    private static readonly string[] FantasyNames =
    [
        "Aiden", "Luna", "Zara", "Kai", "Mia", "Leo", "Ella", "Sage", "Ruby", "Finn",
        "Nova", "River", "Iris", "Atlas", "Willow", "Phoenix", "Aria", "Orion", "Ivy", "Storm",
        "Ember", "Ocean", "Jade", "Blaze", "Star", "Forest", "Dawn", "Shadow", "Sky", "Flame",
        "Coral", "Wind", "Stone", "Aurora", "Thunder", "Meadow", "Crystal", "Vale", "Frost", "Sunny",
        "Raven", "Brook", "Cedar", "Aspen", "Rowan", "Sage", "Wren", "Fox", "Bear", "Wolf"
    ];

    private static readonly string[] AdjectiveNames =
    [
        "Brave", "Swift", "Clever", "Kind", "Bold", "Wise", "Gentle", "Strong", "Bright", "Noble",
        "Quick", "Loyal", "Fierce", "Calm", "Smart", "Lucky", "Happy", "Curious", "Daring", "Cheerful"
    ];

    private static readonly Random Random = new();

    /// <summary>
    /// Generate a random fantasy name
    /// </summary>
    /// <returns>A random fantasy name</returns>
    public static string GenerateFantasyName()
    {
        return FantasyNames[Random.Next(FantasyNames.Length)];
    }

    /// <summary>
    /// Generate a random adjective + fantasy name combination
    /// </summary>
    /// <returns>A random adjective + name combination</returns>
    public static string GenerateAdjectiveName()
    {
        var adjective = AdjectiveNames[Random.Next(AdjectiveNames.Length)];
        var name = FantasyNames[Random.Next(FantasyNames.Length)];
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