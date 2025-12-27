namespace Mystira.App.Application.Helpers;

/// <summary>
/// Shared string manipulation extension methods
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// Converts a string to title case, handling underscores and hyphens as word separators
    /// </summary>
    /// <param name="input">The input string</param>
    /// <returns>The string in title case</returns>
    public static string ToTitleCase(this string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        var words = input.Split(' ', '_', '-');
        for (int i = 0; i < words.Length; i++)
        {
            if (words[i].Length > 0)
            {
                words[i] = char.ToUpper(words[i][0]) + words[i].Substring(1).ToLower();
            }
        }
        return string.Join(" ", words);
    }

    /// <summary>
    /// Truncates a string to the specified maximum length with optional suffix
    /// </summary>
    /// <param name="input">The input string</param>
    /// <param name="maxLength">Maximum length of the result</param>
    /// <param name="suffix">Suffix to append when truncated (default: "...")</param>
    /// <returns>The truncated string</returns>
    public static string Truncate(this string input, int maxLength, string suffix = "...")
    {
        if (string.IsNullOrEmpty(input) || input.Length <= maxLength)
        {
            return input;
        }

        return input.Substring(0, maxLength - suffix.Length) + suffix;
    }

    /// <summary>
    /// Converts a string to snake_case
    /// </summary>
    /// <param name="input">The input string</param>
    /// <returns>The string in snake_case</returns>
    public static string ToSnakeCase(this string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        return string.Concat(
            input.Select((c, i) =>
                i > 0 && char.IsUpper(c)
                    ? "_" + char.ToLower(c)
                    : char.ToLower(c).ToString()
            )
        );
    }
}
