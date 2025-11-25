namespace Mystira.DevHub.CLI.Models;

public class CommandResponse
{
    public bool Success { get; set; }
    public object? Result { get; set; }
    public string? Error { get; set; }
    public string? Message { get; set; }

    public static CommandResponse Ok(object? result = null, string? message = null)
    {
        return new CommandResponse
        {
            Success = true,
            Result = result,
            Message = message
        };
    }

    public static CommandResponse Fail(string error, string? message = null)
    {
        return new CommandResponse
        {
            Success = false,
            Error = error,
            Message = message
        };
    }
}
