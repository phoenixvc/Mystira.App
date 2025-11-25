using Mystira.App.Contracts.Requests.CharacterMaps;
using Mystira.App.Domain.Models;

namespace Mystira.App.Api.Services;

/// <summary>
/// DEPRECATED: This service violates hexagonal architecture.
/// Controllers should use IMediator (CQRS pattern) instead.
/// </summary>
/// <remarks>
/// Migration guide:
/// - GetAllCharacterMapsAsync → GetAllCharacterMapsQuery
/// - GetCharacterMapAsync → GetCharacterMapQuery
/// - CreateCharacterMapAsync → (Create CreateCharacterMapCommand if needed)
/// - UpdateCharacterMapAsync → (Create UpdateCharacterMapCommand if needed)
/// - DeleteCharacterMapAsync → (Create DeleteCharacterMapCommand if needed)
/// - ExportCharacterMapsAsYamlAsync → (Create ExportCharacterMapsQuery if needed)
/// - ImportCharacterMapsFromYamlAsync → (Create ImportCharacterMapsCommand if needed)
/// See ARCHITECTURAL_REFACTORING_PLAN.md for details.
/// </remarks>
[Obsolete("Use IMediator with CQRS queries/commands instead. See ARCHITECTURAL_REFACTORING_PLAN.md")]
public interface ICharacterMapApiService
{
    Task<List<CharacterMap>> GetAllCharacterMapsAsync();
    Task<CharacterMap?> GetCharacterMapAsync(string id);
    Task<CharacterMap> CreateCharacterMapAsync(CreateCharacterMapRequest request);
    Task<CharacterMap?> UpdateCharacterMapAsync(string id, UpdateCharacterMapRequest request);
    Task<bool> DeleteCharacterMapAsync(string id);
    Task<string> ExportCharacterMapsAsYamlAsync();
    Task<List<CharacterMap>> ImportCharacterMapsFromYamlAsync(Stream yamlStream);
}
