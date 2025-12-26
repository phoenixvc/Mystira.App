namespace Mystira.App.Application.CQRS.Common.Responses;

/// <summary>
/// Simple response for commands that return success status and message.
/// </summary>
public record CommandResponse(
    bool Success,
    string Message
);
