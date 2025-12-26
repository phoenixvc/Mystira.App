namespace Mystira.App.Domain.Exceptions;

/// <summary>
/// Exception thrown when validation fails.
/// Maps to HTTP 400 Bad Request.
/// </summary>
public class ValidationException : DomainException
{
    public IReadOnlyList<ValidationError> Errors { get; }

    public ValidationException(string message)
        : base(message, "VALIDATION_FAILED")
    {
        Errors = Array.Empty<ValidationError>();
    }

    public ValidationException(string field, string message)
        : base(message, "VALIDATION_FAILED",
            new Dictionary<string, object>
            {
                ["field"] = field
            })
    {
        Errors = new[] { new ValidationError(field, message) };
    }

    public ValidationException(IEnumerable<ValidationError> errors)
        : base("One or more validation errors occurred.", "VALIDATION_FAILED",
            new Dictionary<string, object>
            {
                ["errors"] = errors.ToList()
            })
    {
        Errors = errors.ToList();
    }
}

/// <summary>
/// Represents a single validation error.
/// </summary>
public record ValidationError(string Field, string Message);
