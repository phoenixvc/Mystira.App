using System;
using System.IO;

namespace Mystira.App.Infrastructure.Azure.Services;

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
