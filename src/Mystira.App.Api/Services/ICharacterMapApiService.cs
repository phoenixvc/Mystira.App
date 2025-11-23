using Mystira.App.Contracts.Requests.CharacterMaps;
using Mystira.App.Domain.Models;

namespace Mystira.App.Api.Services;

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
