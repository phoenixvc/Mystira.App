using System.Linq.Expressions;
using Mystira.App.Domain.Models;
using Mystira.Shared.Data.Repositories;

namespace Mystira.App.Application.Ports.Data;

/// <summary>
/// Repository interface for Scenario entity with domain-specific queries
/// </summary>
public interface IScenarioRepository : IRepository<Scenario>
{
    Task<IEnumerable<Scenario>> GetByAgeGroupAsync(string ageGroup);
    Task<Scenario?> GetByTitleAsync(string title);
    Task<bool> ExistsByTitleAsync(string title);
    IQueryable<Scenario> GetQueryable();
    Task<int> CountAsync(Expression<Func<Scenario, bool>>? predicate = null);
}

