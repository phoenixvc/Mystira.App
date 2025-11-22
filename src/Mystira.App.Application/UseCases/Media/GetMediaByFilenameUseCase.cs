using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports;
using Mystira.App.Domain.Models;
using Mystira.App.Infrastructure.Data.Repositories;
using IMediaAssetRepository = Mystira.App.Infrastructure.Data.Repositories.IMediaAssetRepository;

namespace Mystira.App.Application.UseCases.Media;

/// <summary>
/// Use case for retrieving a media asset by filename (resolves through metadata)
/// </summary>
public class GetMediaByFilenameUseCase
{
    private readonly IMediaAssetRepository _repository;
    private readonly IMediaMetadataService _mediaMetadataService;
    private readonly ILogger<GetMediaByFilenameUseCase> _logger;

    public GetMediaByFilenameUseCase(
        IMediaAssetRepository repository,
        IMediaMetadataService mediaMetadataService,
        ILogger<GetMediaByFilenameUseCase> logger)
    {
        _repository = repository;
        _mediaMetadataService = mediaMetadataService;
        _logger = logger;
    }

    public async Task<MediaAsset?> ExecuteAsync(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("File name is required", nameof(fileName));
        }

        try
        {
            // Get metadata file to resolve filename to media ID
            var metadataFile = await _mediaMetadataService.GetMediaMetadataFileAsync();
            if (metadataFile == null)
            {
                _logger.LogWarning("Media metadata file not found");
                return null;
            }

            // Find metadata entry by filename
            var metadataEntry = metadataFile.Entries.FirstOrDefault(e => e.FileName == fileName);
            if (metadataEntry == null)
            {
                _logger.LogWarning("Media metadata entry not found for filename: {FileName}", fileName);
                return null;
            }

            // Get the media asset by the resolved media ID
            return await _repository.GetByMediaIdAsync(metadataEntry.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting media by filename: {FileName}", fileName);
            return null;
        }
    }
}

