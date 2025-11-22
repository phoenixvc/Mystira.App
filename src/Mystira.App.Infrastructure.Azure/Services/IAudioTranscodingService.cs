using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Mystira.App.Infrastructure.Azure.Services;

/// <summary>
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
