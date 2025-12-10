using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.JSInterop;
using Mystira.App.PWA;
using Mystira.App.PWA.Services;
using Polly;
using Polly.Extensions.Http;

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

// Register singleton cache for API endpoints - solves DelegatingHandler lifetime issues
// This cache is shared across all handler instances and is thread-safe
builder.Services.AddSingleton<IApiEndpointCache, ApiEndpointCache>();

// Register telemetry service for Application Insights tracking
builder.Services.AddScoped<ITelemetryService, TelemetryService>();

// Register API Configuration Service (handles domain persistence across PWA updates)
// This service reads from localStorage and provides the current API URL
builder.Services.AddScoped<IApiConfigurationService, ApiConfigurationService>();

// Register the dynamic API base address handler
// This handler uses the singleton cache - no event subscriptions, no lifetime issues
builder.Services.AddTransient<ApiBaseAddressHandler>();

// Get default API URL from configuration (used as fallback if no persisted URL)
var defaultApiUrl = builder.Configuration.GetValue<string>("ApiConfiguration:DefaultApiBaseUrl")
                    ?? builder.Configuration.GetConnectionString("MystiraApiBaseUrl")
                    ?? "https://api.mystira.app/";

// Validate API configuration at startup
ValidateApiConfiguration(builder.Configuration, defaultApiUrl);

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

// Factory function to create resilience policies for each client
// IMPORTANT: Each client gets its OWN circuit breaker instance to prevent cascade failures
// (If ScenarioApi fails, it shouldn't block AuthApi, etc.)
IAsyncPolicy<HttpResponseMessage> CreateResiliencePolicy(string clientName)
{
    // Retry policy: exponential backoff (2s, 4s, 8s) on transient errors
    var retryPolicy = HttpPolicyExtensions
        .HandleTransientHttpError()
        .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
            onRetry: (outcome, timespan, retryAttempt, _) =>
            {
                Console.WriteLine($"[{clientName}:Retry] Attempt {retryAttempt} after {timespan.TotalSeconds}s - {outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString()}");
            });

    // Circuit breaker: opens after 5 failures, stays open for 30s
    // Each client has its own circuit breaker state
    var circuitBreakerPolicy = HttpPolicyExtensions
        .HandleTransientHttpError()
        .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30),
            onBreak: (outcome, breakDelay) =>
            {
                Console.WriteLine($"[{clientName}:CircuitBreaker] Opened for {breakDelay.TotalSeconds}s - {outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString()}");
            },
            onReset: () => Console.WriteLine($"[{clientName}:CircuitBreaker] Reset"),
            onHalfOpen: () => Console.WriteLine($"[{clientName}:CircuitBreaker] Half-open, testing..."));

    // Timeout policy: 30 second timeout per request
    var timeoutPolicy = Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(30));

    // Combined policy: timeout wraps retry wraps circuit breaker
    return Policy.WrapAsync(timeoutPolicy, retryPolicy, circuitBreakerPolicy);
}

// Register domain-specific API clients with dynamic base address resolution and resilience policies
// Each client uses ApiBaseAddressHandler to resolve URLs from localStorage
// IMPORTANT: Each client gets its own circuit breaker via CreateResiliencePolicy()
builder.Services.AddHttpClient<IScenarioApiClient, ScenarioApiClient>(ConfigureApiHttpClient)
    .AddPolicyHandler(CreateResiliencePolicy("ScenarioApi"))
    .AddHttpMessageHandler<ApiBaseAddressHandler>()
    .AddHttpMessageHandler<AuthHeaderHandler>();

builder.Services.AddHttpClient<IGameSessionApiClient, GameSessionApiClient>(ConfigureApiHttpClient)
    .AddPolicyHandler(CreateResiliencePolicy("GameSessionApi"))
    .AddHttpMessageHandler<ApiBaseAddressHandler>()
    .AddHttpMessageHandler<AuthHeaderHandler>();

builder.Services.AddHttpClient<IUserProfileApiClient, UserProfileApiClient>(ConfigureApiHttpClient)
    .AddPolicyHandler(CreateResiliencePolicy("UserProfileApi"))
    .AddHttpMessageHandler<ApiBaseAddressHandler>()
    .AddHttpMessageHandler<AuthHeaderHandler>();

builder.Services.AddHttpClient<IAuthApiClient, AuthApiClient>(ConfigureApiHttpClient)
    .AddPolicyHandler(CreateResiliencePolicy("AuthApi"))
    .AddHttpMessageHandler<ApiBaseAddressHandler>()
    .AddHttpMessageHandler<AuthHeaderHandler>();

builder.Services.AddHttpClient<IMediaApiClient, MediaApiClient>(ConfigureApiHttpClient)
    .AddPolicyHandler(CreateResiliencePolicy("MediaApi"))
    .AddHttpMessageHandler<ApiBaseAddressHandler>()
    .AddHttpMessageHandler<AuthHeaderHandler>();

builder.Services.AddHttpClient<IAvatarApiClient, AvatarApiClient>(ConfigureApiHttpClient)
    .AddPolicyHandler(CreateResiliencePolicy("AvatarApi"))
    .AddHttpMessageHandler<ApiBaseAddressHandler>()
    .AddHttpMessageHandler<AuthHeaderHandler>();

builder.Services.AddHttpClient<IContentBundleApiClient, ContentBundleApiClient>(ConfigureApiHttpClient)
    .AddPolicyHandler(CreateResiliencePolicy("ContentBundleApi"))
    .AddHttpMessageHandler<ApiBaseAddressHandler>()
    .AddHttpMessageHandler<AuthHeaderHandler>();

builder.Services.AddHttpClient<ICharacterApiClient, CharacterApiClient>(ConfigureApiHttpClient)
    .AddPolicyHandler(CreateResiliencePolicy("CharacterApi"))
    .AddHttpMessageHandler<ApiBaseAddressHandler>()
    .AddHttpMessageHandler<AuthHeaderHandler>();

builder.Services.AddHttpClient<IDiscordApiClient, DiscordApiClient>(ConfigureApiHttpClient)
    .AddPolicyHandler(CreateResiliencePolicy("DiscordApi"))
    .AddHttpMessageHandler<ApiBaseAddressHandler>()
    .AddHttpMessageHandler<AuthHeaderHandler>();

builder.Services.AddHttpClient<IAttributionApiClient, AttributionApiClient>(ConfigureApiHttpClient)
    .AddPolicyHandler(CreateResiliencePolicy("AttributionApi"))
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
        typeof(IDiscordApiClient),
        typeof(IAttributionApiClient)
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

static void ValidateApiConfiguration(IConfiguration configuration, string defaultApiUrl)
{
    var errors = new List<string>();

    // Validate default API URL
    if (string.IsNullOrWhiteSpace(defaultApiUrl))
    {
        errors.Add("DefaultApiBaseUrl is not configured");
    }
    else if (!Uri.TryCreate(defaultApiUrl, UriKind.Absolute, out var uri))
    {
        errors.Add($"DefaultApiBaseUrl is not a valid URL: {defaultApiUrl}");
    }
    else if (uri.Scheme != Uri.UriSchemeHttps && !uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase))
    {
        errors.Add($"DefaultApiBaseUrl must use HTTPS for non-localhost: {defaultApiUrl}");
    }

    // Validate available endpoints if configured
    var endpointsSection = configuration.GetSection("ApiConfiguration:AvailableEndpoints");
    if (endpointsSection.Exists())
    {
        foreach (var child in endpointsSection.GetChildren())
        {
            var url = child["Url"] ?? child["url"];
            var name = child["Name"] ?? child["name"] ?? "unnamed";

            if (string.IsNullOrWhiteSpace(url))
            {
                errors.Add($"Endpoint '{name}' has no URL configured");
                continue;
            }

            if (!Uri.TryCreate(url, UriKind.Absolute, out var endpointUri))
            {
                errors.Add($"Endpoint '{name}' has invalid URL: {url}");
            }
            else if (endpointUri.Scheme != Uri.UriSchemeHttps && !endpointUri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase))
            {
                errors.Add($"Endpoint '{name}' must use HTTPS for non-localhost: {url}");
            }
        }
    }

    // Log warnings for configuration issues (don't fail startup)
    foreach (var error in errors)
    {
        Console.WriteLine($"[Config Warning] {error}");
    }

    if (errors.Count > 0)
    {
        Console.WriteLine($"[Config] Found {errors.Count} configuration warning(s). App will continue but may have issues.");
    }
}
