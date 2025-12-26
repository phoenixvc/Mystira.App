namespace Mystira.App.Domain.Exceptions;

/// <summary>
/// Base exception for all domain-specific exceptions.
/// Provides a consistent structure for error handling across the application.
/// </summary>
public abstract class DomainException : Exception
{
    /// <summary>
    /// A machine-readable error code for programmatic handling.
    /// </summary>
    public string ErrorCode { get; }

    /// <summary>
    /// Additional details about the error for debugging purposes.
    /// </summary>
    public IDictionary<string, object>? Details { get; }

    protected DomainException(string message, string errorCode, IDictionary<string, object>? details = null)
        : base(message)
    {
        ErrorCode = errorCode;
        Details = details;
    }

    protected DomainException(string message, string errorCode, Exception innerException, IDictionary<string, object>? details = null)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
        Details = details;
    }
}
