using Mystira.App.Domain.Models;
using Mystira.App.Infrastructure.Data.Repositories;

namespace Mystira.App.Api.Services;

public class ContentBundleService : IContentBundleService
{
    private readonly IContentBundleRepository _repository;
    private readonly ILogger<ContentBundleService> _logger;

    public ContentBundleService(IContentBundleRepository repository, ILogger<ContentBundleService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<List<ContentBundle>> GetAllAsync()
    {
        var bundles = await _repository.GetAllAsync();
        return bundles.ToList();
    }

    public async Task<List<ContentBundle>> GetByAgeGroupAsync(string ageGroup)
    {
        var bundles = await _repository.GetByAgeGroupAsync(ageGroup);
        return bundles.ToList();
    }
}
