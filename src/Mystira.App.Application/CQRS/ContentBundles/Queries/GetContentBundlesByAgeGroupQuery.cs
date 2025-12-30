using Mystira.Domain.Models;

namespace Mystira.App.Application.CQRS.ContentBundles.Queries;

/// <summary>
/// Query to retrieve content bundles by age group
/// </summary>
public record GetContentBundlesByAgeGroupQuery(string AgeGroup) : IQuery<IEnumerable<ContentBundle>>;
