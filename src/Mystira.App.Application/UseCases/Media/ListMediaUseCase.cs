using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mystira.App.Contracts.Requests.Media;
using Mystira.App.Contracts.Responses.Media;
using Mystira.App.Infrastructure.Data.Repositories;
using IMediaAssetRepository = Mystira.App.Infrastructure.Data.Repositories.IMediaAssetRepository;

namespace Mystira.App.Application.UseCases.Media;

/// <summary>
/// Use case for listing media assets with filtering and pagination
/// </summary>
public class ListMediaUseCase
{
    private readonly IMediaAssetRepository _repository;
    private readonly ILogger<ListMediaUseCase> _logger;

    public ListMediaUseCase(
        IMediaAssetRepository repository,
        ILogger<ListMediaUseCase> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<MediaQueryResponse> ExecuteAsync(MediaQueryRequest request)
    {
        var query = _repository.GetQueryable();

        // Apply filters
        if (!string.IsNullOrEmpty(request.Search))
        {
            query = query.Where(m => m.MediaId.Contains(request.Search) ||
                                    m.Url.Contains(request.Search) ||
                                    (m.Description != null && m.Description.Contains(request.Search)));
        }

        if (!string.IsNullOrEmpty(request.MediaType))
        {
            query = query.Where(m => m.MediaType == request.MediaType);
        }

        if (request.Tags != null && request.Tags.Count > 0)
        {
            foreach (var tag in request.Tags)
            {
                query = query.Where(m => m.Tags.Contains(tag));
            }
        }

        // Apply sorting
        query = request.SortBy?.ToLower() switch
        {
            "filename" => request.SortDescending ? query.OrderByDescending(m => m.Url) : query.OrderBy(m => m.Url),
            "mediatype" => request.SortDescending ? query.OrderByDescending(m => m.MediaType) : query.OrderBy(m => m.MediaType),
            "filesize" => request.SortDescending ? query.OrderByDescending(m => m.FileSizeBytes) : query.OrderBy(m => m.FileSizeBytes),
            "updatedat" => request.SortDescending ? query.OrderByDescending(m => m.UpdatedAt) : query.OrderBy(m => m.UpdatedAt),
            _ => request.SortDescending ? query.OrderByDescending(m => m.CreatedAt) : query.OrderBy(m => m.CreatedAt)
        };

        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);

        var media = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        return new MediaQueryResponse
        {
            Media = media,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalPages = totalPages
        };
    }
}

