using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using System.Text.Json;
using System.Text.Json.Serialization;
using Mystira.App.PWA;
using Mystira.App.PWA.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Configure HttpClient
builder.Services.AddScoped(sp => new HttpClient 
{ 
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) 
});

// Configure API HttpClient
builder.Services.AddHttpClient<IApiClient, ApiClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration.GetValue<string>("ApiBaseUrl") ?? "http://localhost:5260/");
    client.DefaultRequestHeaders.Add("User-Agent", "Mystira/1.0");
});

// Configure JSON serialization with enum string conversion
builder.Services.Configure<JsonSerializerOptions>(options =>
{
    options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.Converters.Add(new JsonStringEnumConverter());
    options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

// In Program.cs for development only, bypass CORS
// if (builder.HostEnvironment.IsDevelopment())
// {
//     builder.Services.AddHttpClient<IApiClient, ApiClient>(client =>
//     {
//         client.BaseAddress = new Uri("https://cors-anywhere.herokuapp.com/https://mystira-app-dev-api.azurewebsites.net//");
//         client.DefaultRequestHeaders.Add("User-Agent", "Mystira/1.0");
//         client.DefaultRequestHeaders.Add("Origin", "http://localhost:7000");
//     });
// }

// Register services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IGameSessionService, GameSessionService>();
builder.Services.AddScoped<IIndexedDbService, IndexedDbService>();
builder.Services.AddScoped<ICharacterAssignmentService, CharacterAssignmentService>();

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
    
    // Verify service registration
    var authService = host.Services.GetService<IAuthService>();
    var apiClient = host.Services.GetService<IApiClient>();
    var gameSessionService = host.Services.GetService<IGameSessionService>();
    var indexedDbService = host.Services.GetService<IIndexedDbService>();
    
    logger.LogInformation("Services registered:");
    logger.LogInformation("- AuthService: {AuthService}", authService?.GetType().Name ?? "Not registered");
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