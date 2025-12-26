namespace Mystira.App.Domain.Exceptions;

/// <summary>
/// Exception thrown when a resource conflict occurs (e.g., duplicate creation).
/// Maps to HTTP 409 Conflict.
/// </summary>
public class ConflictException : DomainException
{
    public string ResourceType { get; }
    public string? ConflictingField { get; }

    public ConflictException(string message)
        : base(message, "RESOURCE_CONFLICT")
    {
        ResourceType = "Unknown";
    }

    public ConflictException(string resourceType, string message)
        : base(message, "RESOURCE_CONFLICT",
            new Dictionary<string, object>
            {
                ["resourceType"] = resourceType
            })
    {
        ResourceType = resourceType;
    }

    public ConflictException(string resourceType, string conflictingField, string message)
        : base(message, "RESOURCE_CONFLICT",
            new Dictionary<string, object>
            {
                ["resourceType"] = resourceType,
                ["conflictingField"] = conflictingField
            })
    {
        ResourceType = resourceType;
        ConflictingField = conflictingField;
    }
}
