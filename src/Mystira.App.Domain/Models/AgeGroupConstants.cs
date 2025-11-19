namespace Mystira.App.Domain.Models;

/// <summary>
/// Constants for all age group categories used throughout the application
/// </summary>
public static class AgeGroupConstants
{
    /// <summary>
    /// Array of all valid age group strings
    /// </summary>
    public static readonly string[] AllAgeGroups = ["1-2", "3-5", "6-9", "10-12", "13-18", "19+"];

    /// <summary>
    /// Mapping of minimum ages to their corresponding age group strings
    /// </summary>
    private static readonly Dictionary<int, string> AgeToGroupMapping = new()
    {
        { 1, "1-2" },
        { 3, "3-5" },
        { 6, "6-9" },
        { 10, "10-12" },
        { 13, "13-18" },
        { 19, "19+" }
    };

    /// <summary>
    /// Get the age group string for a given age
    /// </summary>
    /// <param name="age">The age in years</param>
    /// <returns>The age group string</returns>
    public static string GetAgeGroupForAge(int age)
    {
        return AgeToGroupMapping
            .Where(kvp => kvp.Key <= age)
            .OrderByDescending(kvp => kvp.Key)
            .Select(kvp => kvp.Value)
            .FirstOrDefault() ?? "1-2";
    }

    /// <summary>
    /// Get the display name for an age group
    /// </summary>
    /// <param name="ageGroup">The age group string</param>
    /// <returns>The display name</returns>
    public static string GetDisplayName(string ageGroup)
    {
        return ageGroup switch
        {
            "1-2" => "Ages 1-2 (Toddlers)",
            "3-5" => "Ages 3-5 (Preschoolers)",
            "6-9" => "Ages 6-9 (School Age)",
            "10-12" => "Ages 10-12 (Preteens)",
            "13-18" => "Ages 13-18 (Teens)",
            "19+" => "Ages 19+ (Adults)",
            _ => ageGroup
        };
    }
}
