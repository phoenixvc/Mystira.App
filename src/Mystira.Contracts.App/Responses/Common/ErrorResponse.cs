namespace Mystira.Contracts.App.Responses.Common;

public class ErrorResponse
{
    public string Message { get; set; } = string.Empty;
    public string? Details { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? TraceId { get; set; }

    /// <summary>
    /// Unique error code for easier lookup (e.g., "AZURE_LOCATION_001")
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// Human-readable error category (e.g., "Azure", "Authentication", "Database")
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// List of suggested solutions or next steps for the user
    /// </summary>
    public List<string> Suggestions { get; set; } = new();

    /// <summary>
    /// Related documentation or resource links
    /// </summary>
    public List<ResourceLink> RelatedLinks { get; set; } = new();

    /// <summary>
    /// Whether this error is recoverable by the user
    /// </summary>
    public bool IsRecoverable { get; set; } = true;

    /// <summary>
    /// Suggested action (e.g., "retry", "login", "contact-support", "check-config")
    /// </summary>
    public string? SuggestedAction { get; set; }
}

public class ResourceLink
{
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
}

public class ValidationErrorResponse : ErrorResponse
{
    public Dictionary<string, List<string>> ValidationErrors { get; set; } = new();
}

/// <summary>
/// Extended error response with full troubleshooting context
/// </summary>
public class TroubleshootingErrorResponse : ErrorResponse
{
    /// <summary>
    /// Step-by-step instructions to resolve the issue
    /// </summary>
    public List<TroubleshootingStep> Steps { get; set; } = new();

    /// <summary>
    /// Commands that can be run to diagnose/fix the issue
    /// </summary>
    public List<DiagnosticCommand> DiagnosticCommands { get; set; } = new();

    /// <summary>
    /// Similar errors that users have encountered
    /// </summary>
    public List<string> SimilarIssues { get; set; } = new();
}

public class TroubleshootingStep
{
    public int Order { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Command { get; set; }
    public bool IsOptional { get; set; }
}

public class DiagnosticCommand
{
    public string Description { get; set; } = string.Empty;
    public string Command { get; set; } = string.Empty;
    public string Platform { get; set; } = "any"; // "any", "windows", "linux", "macos"
}

