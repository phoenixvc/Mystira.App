using Mystira.App.PWA.Models;

namespace Mystira.App.PWA.Services;

public interface ICharacterApiClient
{
    Task<Character?> GetCharacterAsync(string id);
    Task<List<Character>?> GetCharactersAsync();
}

