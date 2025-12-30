using Mystira.Domain.Models;

namespace Mystira.App.Application.CQRS.ContentBundles.Queries;

/// <summary>
/// Query to retrieve all content bundles
/// </summary>
public record GetAllContentBundlesQuery() : IQuery<IEnumerable<ContentBundle>>;
