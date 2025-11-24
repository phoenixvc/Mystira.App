using Mystira.App.Application.Interfaces;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.MediaAssets.Queries;

/// <summary>
/// Query to retrieve media asset metadata by ID
/// </summary>
public record GetMediaAssetQuery(string MediaId) : IQuery<MediaAsset?>;
