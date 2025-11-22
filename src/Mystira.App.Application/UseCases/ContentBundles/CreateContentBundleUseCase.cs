using Microsoft.Extensions.Logging;
using Mystira.App.Domain.Models;
using Mystira.App.Infrastructure.Data.Repositories;
using Mystira.App.Infrastructure.Data.UnitOfWork;

namespace Mystira.App.Application.UseCases.ContentBundles;

/// <summary>
/// Use case for creating a new content bundle
/// </summary>
public class CreateContentBundleUseCase
{
    private readonly IContentBundleRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateContentBundleUseCase> _logger;

    public CreateContentBundleUseCase(
        IContentBundleRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<CreateContentBundleUseCase> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ContentBundle> ExecuteAsync(
        string title,
        string description,
        List<string> scenarioIds,
        string imageId,
        List<BundlePrice> prices,
        bool isFree,
        string ageGroup)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Title cannot be null or empty", nameof(title));
        }

        if (scenarioIds == null)
        {
            throw new ArgumentNullException(nameof(scenarioIds));
        }

        var bundle = new ContentBundle
        {
            Id = Guid.NewGuid().ToString("N"),
            Title = title,
            Description = description ?? string.Empty,
            ScenarioIds = scenarioIds,
            ImageId = imageId ?? string.Empty,
            Prices = prices ?? new List<BundlePrice>(),
            IsFree = isFree,
            AgeGroup = ageGroup ?? string.Empty
        };

        await _repository.AddAsync(bundle);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Created content bundle: {BundleId} - {Title}", bundle.Id, bundle.Title);
        return bundle;
    }
}

