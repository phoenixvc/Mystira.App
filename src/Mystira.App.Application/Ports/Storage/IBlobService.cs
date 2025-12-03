namespace Mystira.App.Application.Ports.Storage;

/// <summary>
/// Port interface for blob storage operations (platform-agnostic).
/// Implementations can use Azure Blob Storage, AWS S3, local file system, etc.
/// </summary>
public interface IBlobService
{
    Task<string> UploadMediaAsync(Stream fileStream, string fileName, string contentType);
    Task<string> GetMediaUrlAsync(string blobName);
    Task<bool> DeleteMediaAsync(string blobName);
    Task<List<string>> ListMediaAsync(string prefix = "");
    Task<Stream?> DownloadMediaAsync(string blobName);
}
