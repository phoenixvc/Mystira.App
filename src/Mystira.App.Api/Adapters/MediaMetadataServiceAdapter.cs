using Mystira.App.Application.Ports;
using Mystira.App.Contracts.Models;
using Mystira.App.Api.Services;

namespace Mystira.App.Api.Adapters;

/// <summary>
/// Adapter that adapts API.Services.IMediaMetadataService to Application.Ports.IMediaMetadataService
/// </summary>
public class MediaMetadataServiceAdapter : Application.Ports.IMediaMetadataService
{
    private readonly Services.IMediaMetadataService _apiService;

    public MediaMetadataServiceAdapter(Services.IMediaMetadataService apiService)
    {
        _apiService = apiService;
    }

    public async Task<MediaMetadataFile?> GetMediaMetadataFileAsync()
    {
        var apiFile = await _apiService.GetMediaMetadataFileAsync();
        return apiFile == null ? null : ConvertToContractsFile(apiFile);
    }

    public async Task<MediaMetadataFile> UpdateMediaMetadataFileAsync(MediaMetadataFile metadataFile)
    {
        var apiFile = ConvertToApiFile(metadataFile);
        var result = await _apiService.UpdateMediaMetadataFileAsync(apiFile);
        return ConvertToContractsFile(result);
    }

    public async Task<MediaMetadataFile> AddMediaMetadataEntryAsync(MediaMetadataEntry entry)
    {
        var apiEntry = ConvertToApiEntry(entry);
        var result = await _apiService.AddMediaMetadataEntryAsync(apiEntry);
        return ConvertToContractsFile(result);
    }

    public async Task<MediaMetadataFile> UpdateMediaMetadataEntryAsync(string entryId, MediaMetadataEntry entry)
    {
        var apiEntry = ConvertToApiEntry(entry);
        var result = await _apiService.UpdateMediaMetadataEntryAsync(entryId, apiEntry);
        return ConvertToContractsFile(result);
    }

    public async Task<MediaMetadataFile> RemoveMediaMetadataEntryAsync(string entryId)
    {
        var result = await _apiService.RemoveMediaMetadataEntryAsync(entryId);
        return ConvertToContractsFile(result);
    }

    public async Task<MediaMetadataEntry?> GetMediaMetadataEntryAsync(string entryId)
    {
        var apiEntry = await _apiService.GetMediaMetadataEntryAsync(entryId);
        return apiEntry == null ? null : ConvertToContractsEntry(apiEntry);
    }

    public async Task<MediaMetadataFile> ImportMediaMetadataEntriesAsync(string jsonData, bool overwriteExisting = false)
    {
        var result = await _apiService.ImportMediaMetadataEntriesAsync(jsonData, overwriteExisting);
        return ConvertToContractsFile(result);
    }

    private static Models.MediaMetadataFile ConvertToApiFile(MediaMetadataFile contractsFile)
    {
        return new Models.MediaMetadataFile
        {
            Id = contractsFile.Id,
            Entries = contractsFile.Entries.Select(ConvertToApiEntry).ToList(),
            CreatedAt = contractsFile.CreatedAt,
            UpdatedAt = contractsFile.UpdatedAt,
            Version = "1.0"
        };
    }

    private static MediaMetadataFile ConvertToContractsFile(Models.MediaMetadataFile apiFile)
    {
        return new MediaMetadataFile
        {
            Id = apiFile.Id,
            Entries = apiFile.Entries.Select(ConvertToContractsEntry).ToList(),
            CreatedAt = apiFile.CreatedAt,
            UpdatedAt = apiFile.UpdatedAt
        };
    }

    private static Models.MediaMetadataEntry ConvertToApiEntry(MediaMetadataEntry contractsEntry)
    {
        return new Models.MediaMetadataEntry
        {
            Id = contractsEntry.Id,
            Title = contractsEntry.Title,
            FileName = contractsEntry.FileName,
            Type = contractsEntry.Type,
            Description = contractsEntry.Description,
            AgeRating = contractsEntry.AgeRating,
            SubjectReferenceId = contractsEntry.SubjectReferenceId,
            ClassificationTags = contractsEntry.ClassificationTags.Select(t => new Models.ClassificationTag
            {
                Key = t.Key,
                Value = t.Value
            }).ToList(),
            Modifiers = contractsEntry.Modifiers.Select(m => new Models.Modifier
            {
                Key = m.Key,
                Value = m.Value
            }).ToList(),
            Loopable = contractsEntry.Loopable
        };
    }

    private static MediaMetadataEntry ConvertToContractsEntry(Models.MediaMetadataEntry apiEntry)
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

