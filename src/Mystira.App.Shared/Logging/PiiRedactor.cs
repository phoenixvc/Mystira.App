using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Mystira.App.Shared.Logging;

/// <summary>
/// Utility for redacting Personally Identifiable Information (PII) from logs
/// to ensure COPPA and GDPR compliance.
/// </summary>
public static partial class PiiRedactor
{
    /// <summary>
    /// Redacts an email address by showing only the domain.
    /// Example: john.doe@example.com → ***@example.com
    /// </summary>
    public static string RedactEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return "[empty]";
        }

        var atIndex = email.IndexOf('@');
        if (atIndex <= 0)
        {
            return "[invalid-email]";
        }

        var domain = email.Substring(atIndex);
        return $"***{domain}";
    }

    /// <summary>
    /// Creates a consistent hash of an email for correlation without exposing PII.
    /// </summary>
    public static string HashEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return "[empty]";
        }

        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(email.ToLowerInvariant()));
        var hashString = Convert.ToHexString(hash);
        return $"user-{hashString[..8]}"; // Use first 8 chars for brevity
    }

    /// <summary>
    /// Redacts a display name by showing only the first initial.
    /// Example: "John Doe" → "J***"
    /// </summary>
    public static string RedactDisplayName(string? displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            return "[empty]";
        }

        return $"{displayName[0]}***";
    }

    /// <summary>
    /// Redacts any email addresses found in a string.
    /// Useful for sanitizing log messages that may contain emails.
    /// </summary>
    public static string RedactEmailsInString(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return text ?? string.Empty;
        }

        return EmailRegex().Replace(text, match => RedactEmail(match.Value));
    }

    /// <summary>
    /// Creates a COPPA-compliant log entry for user actions.
    /// Returns: user-hash, action, timestamp (no PII).
    /// </summary>
    public static string CreateSafeLogEntry(string? email, string action)
    {
        var userHash = HashEmail(email);
        return $"[{userHash}] {action}";
    }

    [GeneratedRegex(@"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b", RegexOptions.Compiled)]
    private static partial Regex EmailRegex();
}
