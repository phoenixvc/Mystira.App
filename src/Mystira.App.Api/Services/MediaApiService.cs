using Mystira.App.Api.Models;
using Mystira.App.Application.UseCases.Media;
using Mystira.App.Contracts.Requests.Media;
using Mystira.App.Contracts.Responses.Media;
using Mystira.App.Domain.Models;

namespace Mystira.App.Api.Services;

/// <summary>
/// Service for managing media assets - delegates to use cases
/// NOTE: This service violates architectural rules (services should not exist in API layer).
/// Controllers should call use cases directly. This is kept temporarily for backward compatibility.
/// </summary>
public class MediaApiService : IMediaApiService
{
    private readonly GetMediaUseCase _getMediaUseCase;
    private readonly GetMediaByFilenameUseCase _getMediaByFilenameUseCase;
    private readonly ListMediaUseCase _listMediaUseCase;
    private readonly UploadMediaUseCase _uploadMediaUseCase;
    private readonly UpdateMediaMetadataUseCase _updateMediaMetadataUseCase;
    private readonly DeleteMediaUseCase _deleteMediaUseCase;
    private readonly DownloadMediaUseCase _downloadMediaUseCase;
    private readonly IMediaUploadService _uploadService; // Temporary - for bulk upload
    private readonly IMediaQueryService _queryService; // Temporary - for validation/stats
    private readonly ILogger<MediaApiService> _logger;

    public MediaApiService(
        GetMediaUseCase getMediaUseCase,
        GetMediaByFilenameUseCase getMediaByFilenameUseCase,
        ListMediaUseCase listMediaUseCase,
        UploadMediaUseCase uploadMediaUseCase,
        UpdateMediaMetadataUseCase updateMediaMetadataUseCase,
        DeleteMediaUseCase deleteMediaUseCase,
        DownloadMediaUseCase downloadMediaUseCase,
        IMediaUploadService uploadService,
        IMediaQueryService queryService,
        ILogger<MediaApiService> logger)
    {
        _getMediaUseCase = getMediaUseCase;
        _getMediaByFilenameUseCase = getMediaByFilenameUseCase;
        _listMediaUseCase = listMediaUseCase;
        _uploadMediaUseCase = uploadMediaUseCase;
        _updateMediaMetadataUseCase = updateMediaMetadataUseCase;
        _deleteMediaUseCase = deleteMediaUseCase;
        _downloadMediaUseCase = downloadMediaUseCase;
        _uploadService = uploadService;
        _queryService = queryService;
        _logger = logger;
    }

    // Query methods - delegate to use cases
    public async Task<Mystira.App.Api.Models.MediaQueryResponse> GetMediaAsync(Mystira.App.Api.Models.MediaQueryRequest request)
    {
        // Convert Api.Models.MediaQueryRequest to Contracts.MediaQueryRequest
        var contractsRequest = new Mystira.App.Contracts.Requests.Media.MediaQueryRequest
        {
            Page = request.Page,
            PageSize = request.PageSize,
            Search = request.Search,
            MediaType = request.MediaType,
            Tags = request.Tags,
            SortBy = request.SortBy,
            SortDescending = request.SortDescending
        };

        var contractsResponse = await _listMediaUseCase.ExecuteAsync(contractsRequest);

        // Convert Contracts.MediaQueryResponse back to Api.Models.MediaQueryResponse
        return new Mystira.App.Api.Models.MediaQueryResponse
        {
            Media = contractsResponse.Media,
            TotalCount = contractsResponse.TotalCount,
            Page = contractsResponse.Page,
            PageSize = contractsResponse.PageSize,
            TotalPages = contractsResponse.TotalPages
        };
    }

    public Task<MediaAsset?> GetMediaByIdAsync(string mediaId) =>
        _getMediaUseCase.ExecuteAsync(mediaId);

    public Task<(Stream stream, string contentType, string fileName)?> GetMediaFileAsync(string mediaId) =>
        _downloadMediaUseCase.ExecuteAsync(mediaId);

    public Task<MediaAsset?> GetMediaByFileNameAsync(string fileName) =>
        _getMediaByFilenameUseCase.ExecuteAsync(fileName);

    public async Task<string?> GetMediaUrlAsync(string fileName)
    {
        var mediaAsset = await _getMediaByFilenameUseCase.ExecuteAsync(fileName);
        return mediaAsset?.Url;
    }

    public Task<MediaValidationResult> ValidateMediaReferencesAsync(List<string> mediaReferences) =>
        _queryService.ValidateMediaReferencesAsync(mediaReferences);

    public Task<MediaUsageStats> GetMediaUsageStatsAsync() =>
        _queryService.GetMediaUsageStatsAsync();

    // Upload methods - delegate to use cases
    public async Task<MediaAsset> UploadMediaAsync(IFormFile file, string mediaId, string mediaType, string? description = null, List<string>? tags = null)
    {
        // Convert IFormFile to UploadMediaRequest
        var request = new UploadMediaRequest
        {
            FileStream = file.OpenReadStream(),
            FileName = file.FileName,
            ContentType = file.ContentType ?? "application/octet-stream",
            FileSizeBytes = file.Length,
            MediaId = mediaId,
            MediaType = mediaType,
            Description = description,
            Tags = tags
        };

        return await _uploadMediaUseCase.ExecuteAsync(request);
    }

    public Task<BulkUploadResult> BulkUploadMediaAsync(IFormFile[] files, bool autoDetectType = true, bool overwriteExisting = false) =>
        _uploadService.BulkUploadMediaAsync(files, autoDetectType, overwriteExisting);

    // Management methods - delegate to use cases
    public async Task<Domain.Models.MediaAsset> UpdateMediaAsync(string mediaId, Mystira.App.Api.Models.MediaUpdateRequest updateData)
    {
        // Convert Api.Models.MediaUpdateRequest to Contracts.MediaUpdateRequest
        var contractsRequest = new Mystira.App.Contracts.Requests.Media.MediaUpdateRequest
        {
            Description = updateData.Description,
            Tags = updateData.Tags,
            MediaType = updateData.MediaType
        };

        return await _updateMediaMetadataUseCase.ExecuteAsync(mediaId, contractsRequest);
    }

    public Task<bool> DeleteMediaAsync(string mediaId) =>
        _deleteMediaUseCase.ExecuteAsync(mediaId);
}
