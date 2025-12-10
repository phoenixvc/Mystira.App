using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.JSInterop;
using Mystira.App.PWA;
using Mystira.App.PWA.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Configure HttpClient for general use (e.g., fetching static assets)
builder.Services.AddScoped(_ => new HttpClient
{
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
});

// Register the auth header handler
builder.Services.AddScoped<AuthHeaderHandler>();

// Register API Configuration Service (handles domain persistence across PWA updates)
// This service reads from localStorage and provides the current API URL
builder.Services.AddScoped<IApiConfigurationService, ApiConfigurationService>();

// Register the dynamic API base address handler
// This handler resolves the API URL at request time from ApiConfigurationService
builder.Services.AddScoped<ApiBaseAddressHandler>();

// Get default API URL from configuration (used as fallback if no persisted URL)
var defaultApiUrl = builder.Configuration.GetValue<string>("ApiConfiguration:DefaultApiBaseUrl")
                    ?? builder.Configuration.GetConnectionString("MystiraApiBaseUrl")
                    ?? "https://api.mystira.app/";

// Log API configuration details
var environment = builder.Configuration.GetValue<string>("ApiConfiguration:Environment") ?? "Unknown";
var allowSwitching = builder.Configuration.GetValue<bool>("ApiConfiguration:AllowEndpointSwitching");
Console.WriteLine($"Environment: {environment}, Default API: {defaultApiUrl}, Endpoint switching allowed: {allowSwitching}");

// Helper to configure API HttpClients with dynamic base address resolution
// The ApiBaseAddressHandler will resolve the actual URL from localStorage at request time
void ConfigureApiHttpClient(HttpClient client)
{
    // Set a placeholder base address - the ApiBaseAddressHandler will override this
    // with the persisted URL from localStorage (if available) at request time
    client.BaseAddress = new Uri(defaultApiUrl);
    client.DefaultRequestHeaders.Add("User-Agent", "Mystira/1.0");
}

// Register domain-specific API clients with dynamic base address resolution
// Each client uses ApiBaseAddressHandler to resolve URLs from localStorage
builder.Services.AddHttpClient<IScenarioApiClient, ScenarioApiClient>(ConfigureApiHttpClient)
    .AddHttpMessageHandler<ApiBaseAddressHandler>()
    .AddHttpMessageHandler<AuthHeaderHandler>();

builder.Services.AddHttpClient<IGameSessionApiClient, GameSessionApiClient>(ConfigureApiHttpClient)
    .AddHttpMessageHandler<ApiBaseAddressHandler>()
    .AddHttpMessageHandler<AuthHeaderHandler>();

builder.Services.AddHttpClient<IUserProfileApiClient, UserProfileApiClient>(ConfigureApiHttpClient)
    .AddHttpMessageHandler<ApiBaseAddressHandler>()
    .AddHttpMessageHandler<AuthHeaderHandler>();

builder.Services.AddHttpClient<IAuthApiClient, AuthApiClient>(ConfigureApiHttpClient)
    .AddHttpMessageHandler<ApiBaseAddressHandler>()
    .AddHttpMessageHandler<AuthHeaderHandler>();

builder.Services.AddHttpClient<IMediaApiClient, MediaApiClient>(ConfigureApiHttpClient)
    .AddHttpMessageHandler<ApiBaseAddressHandler>()
    .AddHttpMessageHandler<AuthHeaderHandler>();

builder.Services.AddHttpClient<IAvatarApiClient, AvatarApiClient>(ConfigureApiHttpClient)
    .AddHttpMessageHandler<ApiBaseAddressHandler>()
    .AddHttpMessageHandler<AuthHeaderHandler>();

builder.Services.AddHttpClient<IContentBundleApiClient, ContentBundleApiClient>(ConfigureApiHttpClient)
    .AddHttpMessageHandler<ApiBaseAddressHandler>()
    .AddHttpMessageHandler<AuthHeaderHandler>();

builder.Services.AddHttpClient<ICharacterApiClient, CharacterApiClient>(ConfigureApiHttpClient)
    .AddHttpMessageHandler<ApiBaseAddressHandler>()
    .AddHttpMessageHandler<AuthHeaderHandler>();

builder.Services.AddHttpClient<IDiscordApiClient, DiscordApiClient>(ConfigureApiHttpClient)
    .AddHttpMessageHandler<ApiBaseAddressHandler>()
    .AddHttpMessageHandler<AuthHeaderHandler>();

// Register main ApiClient that composes all domain clients
builder.Services.AddScoped<IApiClient, ApiClient>();

// Configure JSON serialization with enum string conversion
builder.Services.Configure<JsonSerializerOptions>(options =>
{
    options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.Converters.Add(new JsonStringEnumConverter());
    options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

// Register services
builder.Services.AddScoped<ITokenProvider, LocalStorageTokenProvider>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IProfileService, ProfileService>();
builder.Services.AddScoped<IGameSessionService, GameSessionService>();
builder.Services.AddScoped<IIndexedDbService, IndexedDbService>();
builder.Services.AddScoped<ICharacterAssignmentService, CharacterAssignmentService>();
builder.Services.AddSingleton<IImageCacheService, ImageCacheService>();

// UI Services
builder.Services.AddScoped<ToastService>();

// Logging configuration
builder.Logging.SetMinimumLevel(LogLevel.Information);
builder.Logging.AddFilter("Microsoft.AspNetCore.Components.WebAssembly", LogLevel.Warning);

try
{
    Console.WriteLine("Starting Mystira...");

    var host = builder.Build();

    // Initialize services
    var logger = host.Services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Mystira PWA starting up");

    // Set IsDevelopment flag for all API clients
    var isDevelopment = builder.HostEnvironment.IsDevelopment();
    SetDevelopmentModeForApiClients(host.Services, isDevelopment, logger);

    if (isDevelopment)
    {
        logger.LogInformation("Running in Development mode. API connection errors will include helpful startup instructions.");
    }

    // Verify service registration
    var authService = host.Services.GetService<IAuthService>();
    var profileService = host.Services.GetService<IProfileService>();
    var apiClient = host.Services.GetService<IApiClient>();
    var gameSessionService = host.Services.GetService<IGameSessionService>();
    var indexedDbService = host.Services.GetService<IIndexedDbService>();

    logger.LogInformation("Services registered:");
    logger.LogInformation("- AuthService: {AuthService}", authService?.GetType().Name ?? "Not registered");
    logger.LogInformation("- ProfileService: {ProfileService}", profileService?.GetType().Name ?? "Not registered");
    logger.LogInformation("- ApiClient: {ApiClient}", apiClient?.GetType().Name ?? "Not registered");
    logger.LogInformation("- GameSessionService: {GameSessionService}", gameSessionService?.GetType().Name ?? "Not registered");
    logger.LogInformation("- IndexedDbService: {IndexedDbService}", indexedDbService?.GetType().Name ?? "Not registered");

    await host.RunAsync();
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error starting Mystira: {ex.Message}");
    Console.Error.WriteLine($"Stack trace: {ex.StackTrace}");
    throw;
}

static void SetDevelopmentModeForApiClients(IServiceProvider services, bool isDevelopment, ILogger logger)
{
    // Create a scope to get scoped services
    using var scope = services.CreateScope();
    var scopedServices = scope.ServiceProvider;

    // Get all registered services and check if they derive from BaseApiClient
    var apiClientTypes = new[]
    {
        typeof(IScenarioApiClient),
        typeof(IGameSessionApiClient),
        typeof(IUserProfileApiClient),
        typeof(IAuthApiClient),
        typeof(IMediaApiClient),
        typeof(IAvatarApiClient),
        typeof(IContentBundleApiClient),
        typeof(ICharacterApiClient),
        typeof(IDiscordApiClient)
    };

    foreach (var interfaceType in apiClientTypes)
    {
        try
        {
            var service = scopedServices.GetService(interfaceType);
            if (service is BaseApiClient apiClient)
            {
                apiClient.SetDevelopmentMode(isDevelopment);
            }
        }
        catch (InvalidOperationException)
        {
            // Service may not be registered or has an unresolved dependency
            // This is acceptable as not all API clients may be configured in all environments
        }
        catch (Exception ex)
        {
            // Log unexpected errors during service resolution that are not related to registration
            logger.LogWarning(ex, "Unexpected error setting development mode for {ServiceType}", interfaceType.Name);
        }
    }
}
