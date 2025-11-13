namespace Mystira.App.Domain.Models;

/// <summary>
/// Badge configuration for different age groups
/// </summary>
public static class BadgeThresholds
{
    public static readonly Dictionary<AgeGroup, Dictionary<string, float>> AgeGroupThresholds = new()
    {
        {
            AgeGroup.Parse("school"), // Ages 6-9
            new Dictionary<string, float>
            {
                { "kindness", 0.5f },
                { "honesty", 0.4f },
                { "bravery", 0.6f },
                { "sharing", 0.3f }
            }
        },
        {
            AgeGroup.Parse("preteens"), // Ages 10-12
            new Dictionary<string, float>
            {
                { "loyalty", 0.6f },
                { "justice", 0.5f },
                { "courage", 0.7f },
                { "empathy", 0.5f },
                { "responsibility", 0.4f }
            }
        },
        {
            AgeGroup.Parse("teens"), // Ages 13-18
            new Dictionary<string, float>
            {
                { "integrity", 0.7f },
                { "self_awareness", 0.6f },
                { "resilience", 0.8f },
                { "ethical_reasoning", 0.5f },
                { "conflict_resolution", 0.6f }
            }
        }
    };
    
    /// <summary>
    /// Get the appropriate badge thresholds for a given age group
    /// </summary>
    /// <param name="ageGroup">The age group to get thresholds for</param>
    /// <returns>Dictionary of compass axis to threshold values</returns>
    public static Dictionary<string, float> GetThresholdsForAgeGroup(AgeGroup ageGroup)
    {
        return AgeGroupThresholds.TryGetValue(ageGroup, out var thresholds) 
            ? thresholds 
            : new Dictionary<string, float>();
    }
    
    /// <summary>
    /// Get the threshold for a specific compass axis and age group
    /// </summary>
    /// <param name="ageGroup">The age group</param>
    /// <param name="axis">The compass axis</param>
    /// <returns>The threshold value, or 0 if not found</returns>
    public static float GetThreshold(AgeGroup ageGroup, string axis)
    {
        var thresholds = GetThresholdsForAgeGroup(ageGroup);
        return thresholds.TryGetValue(axis, out var threshold) ? threshold : 0f;
    }
    
    /// <summary>
    /// Check if a value meets the threshold for a specific axis and age group
    /// </summary>
    /// <param name="ageGroup">The age group</param>
    /// <param name="axis">The compass axis</param>
    /// <param name="value">The current value</param>
    /// <returns>True if the threshold is met</returns>
    public static bool IsThresholdMet(AgeGroup ageGroup, string axis, float value)
    {
        var threshold = GetThreshold(ageGroup, axis);
        return value >= threshold;
    }
}