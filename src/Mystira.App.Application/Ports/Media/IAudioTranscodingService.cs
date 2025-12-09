namespace Mystira.App.Application.Ports.Media;

/// <summary>
/// Port interface for audio transcoding operations.
/// Provides audio transcoding helpers for platform-specific formats.
/// </summary>
public interface IAudioTranscodingService
{
    /// <summary>
    /// Converts a WhatsApp voice note (.waptt/Opus) into a browser-friendly audio stream.
    /// </summary>
    /// <param name="source">The source audio stream.</param>
    /// <param name="originalFileName">Original file name for naming hints.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The converted audio stream or <c>null</c> when conversion fails.</returns>
    Task<AudioTranscodingResult?> ConvertWhatsAppVoiceNoteAsync(Stream source, string originalFileName, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents the outcome of an audio transcoding operation.
/// </summary>
public sealed record AudioTranscodingResult(Stream Stream, string FileName, string ContentType) : IDisposable
{
    public void Dispose()
    {
        Stream.Dispose();
    }
}
