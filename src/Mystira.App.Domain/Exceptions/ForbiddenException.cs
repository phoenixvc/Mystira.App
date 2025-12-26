namespace Mystira.App.Domain.Exceptions;

/// <summary>
/// Exception thrown when access to a resource is forbidden.
/// Maps to HTTP 403 Forbidden.
/// </summary>
public class ForbiddenException : DomainException
{
    public string? Resource { get; }
    public string? RequiredPermission { get; }

    public ForbiddenException(string message)
        : base(message, "ACCESS_FORBIDDEN")
    {
    }

    public ForbiddenException(string resource, string message)
        : base(message, "ACCESS_FORBIDDEN",
            new Dictionary<string, object>
            {
                ["resource"] = resource
            })
    {
        Resource = resource;
    }

    public ForbiddenException(string resource, string requiredPermission, string message)
        : base(message, "ACCESS_FORBIDDEN",
            new Dictionary<string, object>
            {
                ["resource"] = resource,
                ["requiredPermission"] = requiredPermission
            })
    {
        Resource = resource;
        RequiredPermission = requiredPermission;
    }
}
