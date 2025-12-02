using Mystira.App.Application.Ports;
using Mystira.App.Domain.Models;
using ApiModels = Mystira.App.Admin.Api.Models;

namespace Mystira.App.Admin.Api.Adapters;

/// <summary>
/// Adapter that adapts Admin.Api.Services.IMediaMetadataService to Application.Ports.IMediaMetadataService
/// </summary>
public class MediaMetadataServiceAdapter : IMediaMetadataService
{
    private readonly Services.IMediaMetadataService _apiService;

    public MediaMetadataServiceAdapter(Services.IMediaMetadataService apiService)
    {
        _apiService = apiService;
    }

    public async Task<MediaMetadataFile?> GetMediaMetadataFileAsync()
    {
        var apiFile = await _apiService.GetMediaMetadataFileAsync();
        return apiFile == null ? null : ConvertToDomainFile(apiFile);
    }

    public async Task<MediaMetadataFile> UpdateMediaMetadataFileAsync(MediaMetadataFile metadataFile)
    {
        var apiFile = ConvertToApiFile(metadataFile);
        var result = await _apiService.UpdateMediaMetadataFileAsync(apiFile);
        return ConvertToDomainFile(result);
    }

    public async Task<MediaMetadataFile> AddMediaMetadataEntryAsync(MediaMetadataEntry entry)
    {
        var apiEntry = ConvertToApiEntry(entry);
        var result = await _apiService.AddMediaMetadataEntryAsync(apiEntry);
        return ConvertToDomainFile(result);
    }

    public async Task<MediaMetadataFile> UpdateMediaMetadataEntryAsync(string entryId, MediaMetadataEntry entry)
    {
        var apiEntry = ConvertToApiEntry(entry);
        var result = await _apiService.UpdateMediaMetadataEntryAsync(entryId, apiEntry);
        return ConvertToDomainFile(result);
    }

    public async Task<MediaMetadataFile> RemoveMediaMetadataEntryAsync(string entryId)
    {
        var result = await _apiService.RemoveMediaMetadataEntryAsync(entryId);
        return ConvertToDomainFile(result);
    }

    public async Task<MediaMetadataEntry?> GetMediaMetadataEntryAsync(string entryId)
    {
        var apiEntry = await _apiService.GetMediaMetadataEntryAsync(entryId);
        return apiEntry == null ? null : ConvertToDomainEntry(apiEntry);
    }

    public async Task<MediaMetadataFile> ImportMediaMetadataEntriesAsync(string jsonData, bool overwriteExisting = false)
    {
        var result = await _apiService.ImportMediaMetadataEntriesAsync(jsonData, overwriteExisting);
        return ConvertToDomainFile(result);
    }

    private static ApiModels.MediaMetadataFile ConvertToApiFile(MediaMetadataFile domainFile)
    {
        return new ApiModels.MediaMetadataFile
        {
            Id = domainFile.Id,
            Entries = domainFile.Entries.Select(ConvertToApiEntry).ToList(),
            CreatedAt = domainFile.CreatedAt,
            UpdatedAt = domainFile.UpdatedAt,
            Version = domainFile.Version
        };
    }

    private static MediaMetadataFile ConvertToDomainFile(ApiModels.MediaMetadataFile apiFile)
    {
        return new MediaMetadataFile
        {
            Id = apiFile.Id,
            Entries = apiFile.Entries.Select(ConvertToDomainEntry).ToList(),
            CreatedAt = apiFile.CreatedAt,
            UpdatedAt = apiFile.UpdatedAt,
            Version = apiFile.Version
        };
    }

    private static ApiModels.MediaMetadataEntry ConvertToApiEntry(MediaMetadataEntry domainEntry)
    {
        return new ApiModels.MediaMetadataEntry
        {
            Id = domainEntry.Id,
            Title = domainEntry.Title,
            FileName = domainEntry.FileName,
            Type = domainEntry.Type,
            Description = domainEntry.Description,
            AgeRating = domainEntry.AgeRating,
            SubjectReferenceId = domainEntry.SubjectReferenceId,
            ClassificationTags = domainEntry.ClassificationTags.Select(t => new ApiModels.ClassificationTag
            {
                Key = t.Key,
                Value = t.Value
            }).ToList(),
            Modifiers = domainEntry.Modifiers.Select(m => new ApiModels.Modifier
            {
                Key = m.Key,
                Value = m.Value
            }).ToList(),
            Loopable = domainEntry.Loopable
        };
    }

    private static MediaMetadataEntry ConvertToDomainEntry(ApiModels.MediaMetadataEntry apiEntry)
    {
        return new MediaMetadataEntry
        {
            Id = apiEntry.Id,
            Title = apiEntry.Title,
            FileName = apiEntry.FileName,
            Type = apiEntry.Type,
            Description = apiEntry.Description,
            AgeRating = apiEntry.AgeRating,
            SubjectReferenceId = apiEntry.SubjectReferenceId,
            ClassificationTags = apiEntry.ClassificationTags.Select(t => new ClassificationTag
            {
                Key = t.Key,
                Value = t.Value
            }).ToList(),
            Modifiers = apiEntry.Modifiers.Select(m => new Modifier
            {
                Key = m.Key,
                Value = m.Value
            }).ToList(),
            Loopable = apiEntry.Loopable
        };
    }
}

