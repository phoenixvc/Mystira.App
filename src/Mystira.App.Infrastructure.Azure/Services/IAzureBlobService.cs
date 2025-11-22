using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Mystira.App.Infrastructure.Azure.Services;

public interface IAzureBlobService
{
    Task<string> UploadMediaAsync(Stream fileStream, string fileName, string contentType);
    Task<string> GetMediaUrlAsync(string blobName);
    Task<bool> DeleteMediaAsync(string blobName);
    Task<List<string>> ListMediaAsync(string prefix = "");
    Task<Stream?> DownloadMediaAsync(string blobName);
}
