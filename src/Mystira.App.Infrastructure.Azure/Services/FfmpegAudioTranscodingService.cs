using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FFMpegCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mystira.App.Application.Ports.Media;

namespace Mystira.App.Infrastructure.Azure.Services;

/// <summary>
/// Audio transcoding implementation backed by ffmpeg.
/// </summary>
public sealed class FfmpegAudioTranscodingService : IAudioTranscodingService
{
    private readonly ILogger<FfmpegAudioTranscodingService> _logger;
    private readonly AudioTranscodingOptions _options;
    private readonly string _workingDirectory;

    public FfmpegAudioTranscodingService(
        IOptions<AudioTranscodingOptions> options,
        ILogger<FfmpegAudioTranscodingService> logger)
    {
        _logger = logger;
        _options = options.Value ?? new AudioTranscodingOptions();

        if (!string.IsNullOrWhiteSpace(_options.FfmpegBinaryFolder))
        {
            GlobalFFOptions.Configure(cfg => cfg.BinaryFolder = _options.FfmpegBinaryFolder);
        }

        _workingDirectory = !string.IsNullOrWhiteSpace(_options.WorkingDirectory)
            ? _options.WorkingDirectory
            : Path.Combine(Path.GetTempPath(), "mystira-transcoding");
    }

    /// <inheritdoc />
    public async Task<AudioTranscodingResult?> ConvertWhatsAppVoiceNoteAsync(Stream source, string originalFileName, CancellationToken cancellationToken = default)
    {
        if (source == null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        if (string.IsNullOrWhiteSpace(originalFileName))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(originalFileName));
        }

        Directory.CreateDirectory(_workingDirectory);

        var inputPath = Path.Combine(_workingDirectory, $"{Guid.NewGuid():N}.ogg");
        var outputPath = Path.Combine(_workingDirectory, $"{Guid.NewGuid():N}.mp3");

        try
        {
            await using (var inputFile = File.Create(inputPath))
            {
                source.Position = 0;
                await source.CopyToAsync(inputFile, cancellationToken);
            }

            var process = FFMpegArguments
                .FromFileInput(inputPath)
                .OutputToFile(outputPath, overwrite: true, options =>
                    options.WithAudioCodec("libmp3lame"));
            await process.ProcessAsynchronously();

            if (!File.Exists(outputPath))
            {
                _logger.LogWarning("ffmpeg finished without producing output for {File}", originalFileName);
                return null;
            }

            var memoryStream = new MemoryStream(await File.ReadAllBytesAsync(outputPath, cancellationToken));
            memoryStream.Position = 0;

            var convertedFileName = Path.ChangeExtension(Path.GetFileName(originalFileName), ".mp3");
            return new AudioTranscodingResult(memoryStream, convertedFileName, "audio/mpeg");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to convert WhatsApp audio {File}", originalFileName);
            return null;
        }
        finally
        {
            TryDelete(inputPath);
            TryDelete(outputPath);
        }
    }

    private void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to clean up temp file {Path}", path);
        }
    }
}
