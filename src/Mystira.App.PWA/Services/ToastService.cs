// ToastService.cs - Service for managing toast notifications

namespace Mystira.App.PWA.Services;

public class ToastService
{
    public event Action<ToastMessage>? OnShow;
    public event Action? OnClear;

    /// <summary>
    /// Show a success toast notification
    /// </summary>
    public void ShowSuccess(string message, int durationMs = 3000)
    {
        Show(message, ToastType.Success, durationMs);
    }

    /// <summary>
    /// Show an error toast notification
    /// </summary>
    public void ShowError(string message, int durationMs = 5000)
    {
        Show(message, ToastType.Error, durationMs);
    }

    /// <summary>
    /// Show a warning toast notification
    /// </summary>
    public void ShowWarning(string message, int durationMs = 4000)
    {
        Show(message, ToastType.Warning, durationMs);
    }

    /// <summary>
    /// Show an info toast notification
    /// </summary>
    public void ShowInfo(string message, int durationMs = 3000)
    {
        Show(message, ToastType.Info, durationMs);
    }

    /// <summary>
    /// Show a toast notification with custom type
    /// </summary>
    public void Show(string message, ToastType type = ToastType.Info, int durationMs = 3000)
    {
        var toast = new ToastMessage
        {
            Id = Guid.NewGuid(),
            Message = message,
            Type = type,
            DurationMs = durationMs,
            Timestamp = DateTime.UtcNow
        };

        OnShow?.Invoke(toast);
    }

    /// <summary>
    /// Clear all toast notifications
    /// </summary>
    public void Clear()
    {
        OnClear?.Invoke();
    }
}

public class ToastMessage
{
    public Guid Id { get; set; }
    public required string Message { get; set; }
    public ToastType Type { get; set; }
    public int DurationMs { get; set; }
    public DateTime Timestamp { get; set; }
}

public enum ToastType
{
    Success,
    Error,
    Warning,
    Info
}
