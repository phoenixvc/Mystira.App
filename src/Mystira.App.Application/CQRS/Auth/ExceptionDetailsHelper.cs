namespace Mystira.App.Application.CQRS.Auth;

/// <summary>
/// Helper class for formatting exception details for debugging purposes.
/// </summary>
internal static class ExceptionDetailsHelper
{
    /// <summary>
    /// Formats exception details including inner exception information.
    /// </summary>
    /// <param name="ex">The exception to format.</param>
    /// <returns>A formatted string with exception type, message, and inner exception details.</returns>
    public static string FormatExceptionDetails(Exception ex)
    {
        var errorDetails = $"{ex.GetType().Name}: {ex.Message}";
        if (ex.InnerException != null)
        {
            errorDetails += $" | Inner: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}";
        }
        return errorDetails;
    }
}
