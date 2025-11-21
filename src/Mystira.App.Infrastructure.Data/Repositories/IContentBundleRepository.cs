using Mystira.App.Domain.Models;

namespace Mystira.App.Infrastructure.Data.Repositories;

/// <summary>
/// Repository interface for ContentBundle entity with domain-specific queries
/// </summary>
public interface IContentBundleRepository : IRepository<ContentBundle>
{
    Task<IEnumerable<ContentBundle>> GetByAgeGroupAsync(string ageGroup);
}

