using System.Text.RegularExpressions;

namespace Mystira.App.PWA.Services;

internal static class AgeGroupHelper
{
    /// <summary>
    /// Compares scenario age group and profile age group with a primary
    /// case-insensitive string equality and a fallback numeric-bounds match
    /// (e.g., treats "06-09" and "6–9" as equal).
    /// </summary>
    public static bool AgeGroupMatches(string? scenario, string? profile)
    {
        // Primary: case-insensitive string equality
        if (!string.IsNullOrWhiteSpace(scenario) &&
            string.Equals(scenario?.Trim(), profile?.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Fallback: numeric bounds equality
        static bool TryParseBounds(string? s, out int lo, out int hi)
        {
            lo = 0; hi = 0;
            if (string.IsNullOrWhiteSpace(s)) return false;

            // Extract first two integers in order (supports different dash types/spaces)
            var digits = Regex.Matches(s, "\\d+")
                .Select(m => m.Value)
                .ToList();
            if (digits.Count < 2) return false;
            if (!int.TryParse(digits[0], out lo)) return false;
            if (!int.TryParse(digits[1], out hi)) return false;
            if (lo > hi)
            {
                (lo, hi) = (hi, lo);
            }
            return true;
        }

        if (TryParseBounds(scenario, out var slo, out var shi) &&
            TryParseBounds(profile, out var plo, out var phi))
        {
            return slo == plo && shi == phi;
        }

        return false;
    }
}
