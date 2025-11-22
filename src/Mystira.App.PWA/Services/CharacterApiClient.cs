using System.Net.Http.Json;
using Mystira.App.PWA.Models;

namespace Mystira.App.PWA.Services;

/// <summary>
/// API client for character-related operations
/// </summary>
public class CharacterApiClient : BaseApiClient, ICharacterApiClient
{
    public CharacterApiClient(HttpClient httpClient, ILogger<CharacterApiClient> logger, ITokenProvider tokenProvider)
        : base(httpClient, logger, tokenProvider)
    {
    }

    public async Task<Character?> GetCharacterAsync(string id)
    {
        try
        {
            Logger.LogInformation("Fetching character {Id} from API...", id);

            var response = await HttpClient.GetAsync($"api/character/{id}");

            if (response.IsSuccessStatusCode)
            {
                var character = await response.Content.ReadFromJsonAsync<Character>(JsonOptions);
                Logger.LogInformation("Successfully fetched character {Id}", id);
                return character;
            }
            else
            {
                Logger.LogWarning("API request failed with status: {StatusCode}. Character {Id} not available.", response.StatusCode, id);
                return null;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error fetching character {Id} from API.", id);
            return null;
        }
    }

    public async Task<List<Character>?> GetCharactersAsync()
    {
        try
        {
            Logger.LogInformation("Fetching characters from API...");

            var response = await HttpClient.GetAsync("api/charactermaps");

            if (response.IsSuccessStatusCode)
            {
                var characters = await response.Content.ReadFromJsonAsync<List<Character>>(JsonOptions);
                Logger.LogInformation("Successfully fetched {Count} characters", characters?.Count ?? 0);
                return characters;
            }
            else
            {
                Logger.LogWarning("API request failed with status: {StatusCode}. No characters available.", response.StatusCode);
                return null;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error fetching characters from API.");
            return null;
        }
    }
}

