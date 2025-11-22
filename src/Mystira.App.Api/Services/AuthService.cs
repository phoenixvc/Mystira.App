namespace Mystira.App.Api.Services;

public class AuthService : IAuthService
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly ILogger<AuthService> _logger;

    public string? AuthToken { get; private set; }
    public bool IsAuthenticated => !string.IsNullOrEmpty(AuthToken);

    public AuthService(HttpClient httpClient, IConfiguration configuration, ILogger<AuthService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _baseUrl = configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7096";

        // Configure HttpClient
        _httpClient.BaseAddress = new Uri(_baseUrl);
    }

    public async Task<bool> LoginAsync(string username, string password)
    {
        try
        {
            var loginRequest = new
            {
                Username = username,
                Password = password
            };

            var response = await _httpClient.PostAsJsonAsync("/api/auth/login", loginRequest);

            if (response.IsSuccessStatusCode)
            {
                var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
                if (authResponse != null && !string.IsNullOrEmpty(authResponse.Token))
                {
                    AuthToken = authResponse.Token;
                    return true;
                }
            }

            _logger.LogWarning("Failed to authenticate. Status: {StatusCode}", response.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during authentication");
            return false;
        }
    }

    public void Logout()
    {
        AuthToken = null;
    }

    private class AuthResponse
    {
        public string Token { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public DateTime Expiration { get; set; }
    }
}