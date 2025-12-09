using System.Text.RegularExpressions;

namespace Mystira.App.Infrastructure.Discord.Utilities;

/// <summary>
/// Utility class for sanitizing Discord channel names.
/// </summary>
public static partial class ChannelNameSanitizer
{
    /// <summary>
    /// Converts a username or text input into a safe Discord channel name slug.
    /// Only allows ASCII letters (a-z) and digits (0-9), replacing all other characters with dashes.
    /// </summary>
    /// <param name="input">The input string to sanitize.</param>
    /// <returns>A sanitized channel name slug suitable for Discord channels.</returns>
    public static string MakeSafeChannelSlug(string input)
    {
        // Very small sanitiser for Discord channel naming
        // Only allow ASCII letters (a-z, A-Z) and digits (0-9)
        var lower = input.ToLowerInvariant();
        var cleaned = new string(lower
            .Select(ch => (ch >= 'a' && ch <= 'z') || (ch >= '0' && ch <= '9') ? ch : '-')
            .ToArray());

        cleaned = cleaned.Trim('-');

        // Collapse consecutive dashes using regex for better performance
        cleaned = ConsecutiveDashesRegex().Replace(cleaned, "-");

        // Limit length to 88 characters (leaving room for "ticket-" prefix and "-####" suffix)
        const int maxLength = 88;
        if (cleaned.Length > maxLength)
            cleaned = cleaned.Substring(0, maxLength).TrimEnd('-');

        return string.IsNullOrWhiteSpace(cleaned) ? "user" : cleaned;
    }

    [GeneratedRegex("-+")]
    private static partial Regex ConsecutiveDashesRegex();
}
