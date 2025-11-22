namespace Mystira.App.Infrastructure.Azure.Services;

/// <summary>
/// Configuration options for audio transcoding.
/// </summary>
public class AudioTranscodingOptions
{
    public const string SectionName = "AudioTranscoding";

    /// <summary>
    /// Optional path to the folder that contains the ffmpeg binaries.
    /// When not provided, ffmpeg is resolved from the system PATH.
    /// </summary>
    public string? FfmpegBinaryFolder { get; set; }

    /// <summary>
    /// Optional path to a working directory for temporary conversion files.
    /// Defaults to the system temp folder when not specified.
    /// </summary>
    public string? WorkingDirectory { get; set; }
}
