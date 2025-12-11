using System.Text;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Mystira.App.Admin.Api.Adapters;
using Mystira.App.Admin.Api.Services;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Application.Ports.Media;
using Mystira.App.Application.UseCases.Contributors;
using Mystira.App.Application.UseCases.GameSessions;
using Mystira.App.Application.UseCases.Media;
using Mystira.App.Application.UseCases.Scenarios;
using Mystira.App.Application.UseCases.UserProfiles;
using Mystira.App.Domain.Models;
using Mystira.App.Infrastructure.Azure;
using Mystira.App.Infrastructure.Azure.HealthChecks;
using Mystira.App.Infrastructure.Azure.Services;
using Mystira.App.Infrastructure.Data;
using Mystira.App.Infrastructure.Data.Repositories;
using Mystira.App.Infrastructure.Data.Services;
using Mystira.App.Infrastructure.Data.UnitOfWork;
using Mystira.App.Infrastructure.StoryProtocol;
using IUnitOfWork = Mystira.App.Application.Ports.Data.IUnitOfWork;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        // Configure enums to serialize as strings instead of numbers
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// Add HttpClient factory for use cases that need to make HTTP requests
builder.Services.AddHttpClient();

// Configure OpenAPI/Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Mystira Admin API",
        Version = "v1",
        Description = "Admin API for Mystira - Content Management & Administration",
        Contact = new OpenApiContact
        {
            Name = "Mystira Team",
            Email = "support@mystira.app"
        }
    });

    // Add JWT authentication to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    // Fix schema naming conflicts
    c.CustomSchemaIds(type =>
    {
        if (type == typeof(CharacterMetadata))
        {
            return "DomainCharacterMetadata";
        }

        if (type == typeof(Mystira.App.Admin.Api.Models.CharacterMetadata))
        {
            return "ApiCharacterMetadata";
        }

        return type.Name;
    });
});

// Configure Database: Azure Cosmos DB (Cloud) or In-Memory (Local Development)
var cosmosConnectionString = builder.Configuration.GetConnectionString("CosmosDb");
var useCosmosDb = !string.IsNullOrEmpty(cosmosConnectionString);
if (useCosmosDb)
{
    // AZURE CLOUD DATABASE: Production Cosmos DB
    builder.Services.AddDbContext<MystiraAppDbContext>(options =>
    {
        options.UseCosmos(cosmosConnectionString!, "MystiraAppDb", cosmosOptions =>
        {
            // Configure HTTP client timeout to prevent hanging indefinitely
            // Default timeout is too long for startup scenarios
            cosmosOptions.HttpClientFactory(() =>
            {
                var httpClient = new HttpClient(new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                });
                httpClient.Timeout = TimeSpan.FromSeconds(30);
                return httpClient;
            });
            
            // Set request timeout for Cosmos operations
            cosmosOptions.RequestTimeout(TimeSpan.FromSeconds(30));
        })
        .AddInterceptors(new PartitionKeyInterceptor());
    });
}
else
{
    // LOCAL DEVELOPMENT DATABASE: In-Memory for testing/development
    builder.Services.AddDbContext<MystiraAppDbContext>(options =>
        options.UseInMemoryDatabase("MystiraAppInMemoryDb_Local"));
}

// Register DbContext base type for repository dependency injection
builder.Services.AddScoped<DbContext>(sp => sp.GetRequiredService<MystiraAppDbContext>());

// Add Azure Infrastructure Services
builder.Services.AddAzureBlobStorage(builder.Configuration);
builder.Services.Configure<AudioTranscodingOptions>(builder.Configuration.GetSection(AudioTranscodingOptions.SectionName));
// Register Application.Ports.Media.IAudioTranscodingService for use cases
builder.Services.AddSingleton<IAudioTranscodingService, FfmpegAudioTranscodingService>();

// Add Story Protocol Services
builder.Services.AddStoryProtocolServices(builder.Configuration);

// Register Content Bundle admin service
builder.Services.AddScoped<IContentBundleAdminService, ContentBundleAdminService>();

// Configure JWT Authentication
var jwtKey = builder.Configuration["JwtSettings:SecretKey"] ?? "Mystira-app-Development-Secret-Key-2024-Very-Long-For-Security";
var jwtIssuer = builder.Configuration["JwtSettings:Issuer"] ?? "mystira-admin-api";
var jwtAudience = builder.Configuration["JwtSettings:Audience"] ?? "mystira-app";

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = "Cookies";
        options.DefaultSignInScheme = "Cookies";
        options.DefaultChallengeScheme = "Cookies";
    })
    .AddCookie("Cookies", options =>
    {
        options.Cookie.Name = "Mystira.Admin.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
        options.LoginPath = "/admin/login";
        options.LogoutPath = "/admin/logout";
        options.AccessDeniedPath = "/admin/forbidden";
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

// Register application services - Admin API services
builder.Services.AddScoped<IScenarioApiService, ScenarioApiService>();
builder.Services.AddScoped<ICharacterMapApiService, CharacterMapApiService>();
builder.Services.AddScoped<IAppStatusService, AppStatusService>();
builder.Services.AddScoped<IBundleService, BundleService>();
builder.Services.AddScoped<ICharacterMapFileService, CharacterMapFileService>();
builder.Services.AddScoped<IMediaMetadataService, Mystira.App.Admin.Api.Services.MediaMetadataService>();
builder.Services.AddScoped<ICharacterMediaMetadataService, CharacterMediaMetadataService>();
builder.Services.AddScoped<IMediaApiService, MediaApiService>();
builder.Services.AddScoped<IAvatarApiService, AvatarApiService>();
builder.Services.AddScoped<IHealthCheckService, HealthCheckServiceAdapter>();
// Register email service for consistency across all APIs
builder.Services.AddAzureEmailService(builder.Configuration);
// Register Application.Ports.IMediaMetadataService for use cases
builder.Services.AddScoped<Mystira.App.Application.Ports.IMediaMetadataService, MediaMetadataServiceAdapter>();
// Register repositories
builder.Services.AddScoped<IRepository<GameSession>, Repository<GameSession>>();
builder.Services.AddScoped<IGameSessionRepository, GameSessionRepository>();
builder.Services.AddScoped<IRepository<UserProfile>, Repository<UserProfile>>();
builder.Services.AddScoped<IUserProfileRepository, UserProfileRepository>();
builder.Services.AddScoped<IRepository<Account>, Repository<Account>>();
builder.Services.AddScoped<IAccountRepository, AccountRepository>();
builder.Services.AddScoped<IScenarioRepository, ScenarioRepository>();
builder.Services.AddScoped<ICharacterMapRepository, CharacterMapRepository>();
builder.Services.AddScoped<IContentBundleRepository, ContentBundleRepository>();
builder.Services.AddScoped<IUserBadgeRepository, UserBadgeRepository>();
builder.Services.AddScoped<IPendingSignupRepository, PendingSignupRepository>();
builder.Services.AddScoped<IMediaAssetRepository, MediaAssetRepository>();
builder.Services.AddScoped<IMediaMetadataFileRepository, MediaMetadataFileRepository>();
builder.Services.AddScoped<ICharacterMediaMetadataFileRepository, CharacterMediaMetadataFileRepository>();
builder.Services.AddScoped<ICharacterMapFileRepository, CharacterMapFileRepository>();
builder.Services.AddScoped<IAvatarConfigurationFileRepository, AvatarConfigurationFileRepository>();
builder.Services.AddScoped<ICompassAxisRepository, CompassAxisRepository>();
builder.Services.AddScoped<IArchetypeRepository, ArchetypeRepository>();
builder.Services.AddScoped<IEchoTypeRepository, EchoTypeRepository>();
builder.Services.AddScoped<IFantasyThemeRepository, FantasyThemeRepository>();
builder.Services.AddScoped<IAgeGroupRepository, AgeGroupRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Register Master Data Seeder Service
builder.Services.AddScoped<MasterDataSeederService>();

// Register Application Layer Use Cases
// Scenario Use Cases
builder.Services.AddScoped<GetScenariosUseCase>();
builder.Services.AddScoped<GetScenarioUseCase>();
builder.Services.AddScoped<CreateScenarioUseCase>();
builder.Services.AddScoped<UpdateScenarioUseCase>();
builder.Services.AddScoped<DeleteScenarioUseCase>();
builder.Services.AddScoped<ValidateScenarioUseCase>();

// GameSession Use Cases
builder.Services.AddScoped<CreateGameSessionUseCase>();
builder.Services.AddScoped<GetGameSessionUseCase>();
builder.Services.AddScoped<GetGameSessionsByAccountUseCase>();
builder.Services.AddScoped<GetGameSessionsByProfileUseCase>();
builder.Services.AddScoped<GetInProgressSessionsUseCase>();
builder.Services.AddScoped<MakeChoiceUseCase>();
builder.Services.AddScoped<ProgressSceneUseCase>();
builder.Services.AddScoped<PauseGameSessionUseCase>();
builder.Services.AddScoped<ResumeGameSessionUseCase>();
builder.Services.AddScoped<EndGameSessionUseCase>();
builder.Services.AddScoped<SelectCharacterUseCase>();
builder.Services.AddScoped<GetSessionStatsUseCase>();
builder.Services.AddScoped<CheckAchievementsUseCase>();
builder.Services.AddScoped<DeleteGameSessionUseCase>();

// UserProfile Use Cases
builder.Services.AddScoped<CreateUserProfileUseCase>();
builder.Services.AddScoped<UpdateUserProfileUseCase>();
builder.Services.AddScoped<GetUserProfileUseCase>();
builder.Services.AddScoped<DeleteUserProfileUseCase>();

// Media Use Cases
builder.Services.AddScoped<GetMediaUseCase>();
builder.Services.AddScoped<GetMediaByFilenameUseCase>();
builder.Services.AddScoped<ListMediaUseCase>();
builder.Services.AddScoped<UploadMediaUseCase>();
builder.Services.AddScoped<UpdateMediaMetadataUseCase>();
builder.Services.AddScoped<DeleteMediaUseCase>();
builder.Services.AddScoped<DownloadMediaUseCase>();

// Contributor / Story Protocol Use Cases
builder.Services.AddScoped<SetScenarioContributorsUseCase>();
builder.Services.AddScoped<SetBundleContributorsUseCase>();
builder.Services.AddScoped<RegisterScenarioIpAssetUseCase>();
builder.Services.AddScoped<RegisterBundleIpAssetUseCase>();

builder.Services.AddScoped<IGameSessionApiService, GameSessionApiService>();
builder.Services.AddScoped<IAccountApiService, AccountApiService>();

// Configure Health Checks
builder.Services.AddHealthChecks()
    .AddCheck<BlobStorageHealthCheck>("blob_storage")
    .AddCheck<CosmosDbHealthCheck>("cosmos_db", tags: new[] { "ready", "db" });

// Configure CORS for frontend integration (Best Practices)
var policyName = "MystiraAdminPolicy";
builder.Services.AddCors(options =>
{
    options.AddPolicy(policyName, policy =>
    {
        // Get allowed origins from configuration
        var allowedOriginsConfig = builder.Configuration.GetSection("CorsSettings:AllowedOrigins").Get<string>();
        string[] originsToUse;

        if (!string.IsNullOrWhiteSpace(allowedOriginsConfig))
        {
            // Use configured origins
            originsToUse = allowedOriginsConfig.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }
        else
        {
            // Fallback to default origins (development/local + production SWAs + API domains for Swagger UI)
            originsToUse = new[]
            {
                "http://localhost:7001",
                "https://localhost:7001",
                "http://localhost:7000",
                "https://localhost:7000",
                "https://admin.mystiraapp.azurewebsites.net",
                "https://admin.mystira.app",
                "https://mystiraapp.azurewebsites.net",
                "https://mystira.app",
                "https://blue-water-0eab7991e.3.azurestaticapps.net",
                "https://brave-meadow-0ecd87c03.3.azurestaticapps.net",
                // Admin API domains for Swagger UI and internal calls
                "https://adminapi.dev.mystira.app",                           // Dev Admin API
                "https://mys-dev-mystira-adminapi-san.azurewebsites.net",     // Dev Admin API (Azure default domain)
                "https://adminapi.staging.mystira.app",                       // Staging Admin API
                "https://mys-staging-mystira-adminapi-san.azurewebsites.net", // Staging Admin API (Azure default domain)
                "https://adminapi.mystira.app",                               // Production Admin API
                "https://mys-prod-mystira-adminapi-san.azurewebsites.net"     // Production Admin API (Azure default domain)
            };
        }

        // Best Practice: Use WithOrigins (not AllowAnyOrigin) when using AllowCredentials
        // AllowAnyOrigin cannot be used with AllowCredentials - must specify exact origins
        policy.WithOrigins(originsToUse);

        // Best Practice: Specify exact headers instead of AllowAnyHeader
        policy.WithHeaders(
            "Content-Type",
            "Authorization",
            "X-Requested-With",
            "Accept",
            "Origin",
            "User-Agent",
            "Cache-Control",
            "Pragma");

        // Best Practice: Specify exact methods instead of AllowAnyMethod
        policy.WithMethods(
            HttpMethod.Get.Method,
            HttpMethod.Post.Method,
            HttpMethod.Put.Method,
            HttpMethod.Patch.Method,
            HttpMethod.Delete.Method,
            HttpMethod.Options.Method);

        // Allow credentials for authenticated requests (required for cookies/auth headers)
        policy.AllowCredentials();

        // Set preflight cache duration (24 hours)
        policy.SetPreflightMaxAge(TimeSpan.FromHours(24));
    });
});

// Configure logging
builder.Logging.AddConsole();
builder.Logging.AddAzureWebAppDiagnostics();

var app = builder.Build();

var logger = app.Logger;
logger.LogInformation(useCosmosDb ? "Using Azure Cosmos DB (Cloud Database)" : "Using In-Memory Database (Local Development)");

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Mystira Admin API v1");
        c.RoutePrefix = string.Empty; // Serve Swagger UI at root
    });
}

app.UseHttpsRedirection();

app.UseRouting();

// ✅ CORS must be between UseRouting and auth/endpoints
app.UseCors(policyName);
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Map health check endpoints
// /health - checks all dependencies (blob storage, database)
app.MapHealthChecks("/health");

// /health/ready - checks only critical dependencies for readiness (database)
// Use this endpoint in deployment health checks to verify database initialization is complete
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = System.Text.Json.JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                duration = e.Value.Duration.TotalMilliseconds
            }),
            totalDuration = report.TotalDuration.TotalMilliseconds
        });
        await context.Response.WriteAsync(result);
    }
});

// /health/live - simple liveness check (always returns 200 if app is running)
// This endpoint runs no health checks (Predicate = _ => false excludes all checks)
// and always returns Healthy status if the app can respond to requests
app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false // Exclude all health checks - just verify app is responsive
});

// Initialize database (optional, controlled by configuration)
var initializeDatabaseOnStartup = builder.Configuration.GetValue<bool>("InitializeDatabaseOnStartup", defaultValue: false);
var isInMemory = !useCosmosDb;

// Always initialize for in-memory databases (local dev), optional for Cosmos DB (production)
if (initializeDatabaseOnStartup || isInMemory)
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<MystiraAppDbContext>();
    var startupLogger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        startupLogger.LogInformation("Starting database initialization (InitializeDatabaseOnStartup={Init}, InMemory={InMemory})...", initializeDatabaseOnStartup, isInMemory);
        
        // Use Task.WaitAny with timeout for more reliable timeout handling
        // CancellationToken doesn't always work well with Cosmos DB SDK
        var initTask = context.Database.EnsureCreatedAsync();
        var timeoutTask = Task.Delay(TimeSpan.FromSeconds(30));
        var completedTask = await Task.WhenAny(initTask, timeoutTask);
        
        if (completedTask == timeoutTask)
        {
            // Timeout occurred
            startupLogger.LogWarning("Database initialization timed out after 30 seconds. The application will start without database initialization. Ensure Azure Cosmos DB is accessible and configured correctly. Set 'InitializeDatabaseOnStartup'=false to skip this check.");
        }
        else
        {
            // Await the actual task to catch any exceptions
            await initTask;
            startupLogger.LogInformation("Database EnsureCreatedAsync completed successfully");

            // Gate master-data seeding by configuration and environment to avoid Cosmos SDK query issues in some setups
            // Defaults: seed only for InMemory or when explicitly enabled via configuration
            var seedOnStartup = builder.Configuration.GetValue<bool>("SeedMasterDataOnStartup");
            if (seedOnStartup || isInMemory)
            {
                try
                {
                    var seeder = scope.ServiceProvider.GetRequiredService<MasterDataSeederService>();
                    
                    // Also apply timeout to seeding operations
                    var seedTask = seeder.SeedAllAsync();
                    var seedTimeoutTask = Task.Delay(TimeSpan.FromSeconds(60));
                    var seedCompletedTask = await Task.WhenAny(seedTask, seedTimeoutTask);
                    
                    if (seedCompletedTask == seedTimeoutTask)
                    {
                        startupLogger.LogWarning("Master data seeding timed out after 60 seconds. The application will continue to start.");
                    }
                    else
                    {
                        await seedTask;
                        startupLogger.LogInformation("Master data seeding completed (SeedMasterDataOnStartup={Seed}, InMemory={InMemory}).", seedOnStartup, isInMemory);
                    }
                }
                catch (Exception seedEx)
                {
                    // Do not crash the app on seeding failure in Cosmos environments; log and continue
                    startupLogger.LogError(seedEx, "Master data seeding failed. The application will continue to start. Set 'SeedMasterDataOnStartup'=false to skip seeding or use InMemory provider for local dev seeding.");
                }
            }

            startupLogger.LogInformation("Database initialization succeeded. Verified containers for current model are present.");
        }
    }
    catch (Exception ex)
    {
        // Log error but don't crash the app in production - allow health checks to detect the issue
        startupLogger.LogError(ex, "Failed to initialize database during startup. The application will start in degraded mode. Ensure Azure Cosmos DB database 'MystiraAppDb' exists and app identity has permissions to create/read containers. Expected containers include: CompassAxes (PK /Id), BadgeConfigurations (PK /Id), CharacterMaps (PK /Id), ContentBundles (PK /Id), Scenarios (PK /Id), MediaMetadataFiles (PK /Id), CharacterMediaMetadataFiles (PK /Id), CharacterMapFiles (PK /Id), UserProfiles (PK /Id), Accounts (PK /Id), PendingSignups (PK /email). Set 'InitializeDatabaseOnStartup'=false to skip this check.");
        
        // Only fail fast in development/local environments where we expect the database to work
        if (isInMemory)
        {
            throw;
        }
    }
}
else
{
    var startupLogger = app.Services.GetRequiredService<ILogger<Program>>();
    startupLogger.LogInformation("Database initialization skipped (InitializeDatabaseOnStartup=false). Ensure database and containers are pre-configured in Azure.");
}

app.Run();

// Make Program class accessible for testing
namespace Mystira.App.Admin.Api
{
    public class Program { }
}
