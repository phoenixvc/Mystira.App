using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Mystira.App.Api.Adapters;
using Mystira.App.Api.Services;
using Mystira.App.Application.Behaviors;
using Mystira.App.Application.Ports;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Application.Ports.Health;
using Mystira.App.Application.Ports.Media;
using Mystira.App.Application.Ports.Messaging;
using Mystira.App.Application.Services;
using Mystira.App.Application.UseCases.Accounts;
using Mystira.App.Application.UseCases.GameSessions;
using Mystira.App.Application.UseCases.Media;
using Mystira.App.Application.UseCases.Scenarios;
using Mystira.App.Application.UseCases.UserProfiles;
using Mystira.App.Infrastructure.Azure;
using Mystira.App.Infrastructure.Azure.HealthChecks;
using Mystira.App.Infrastructure.Azure.Services;
using Mystira.App.Infrastructure.Data;
using Mystira.App.Infrastructure.Data.Repositories;
using Mystira.App.Infrastructure.Data.Services;
using Mystira.App.Infrastructure.Discord;
using Mystira.App.Infrastructure.Discord.Services;
using Mystira.App.Infrastructure.StoryProtocol;
using Mystira.App.Shared.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        // Configure enums to serialize as strings instead of numbers
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

// Configure OpenAPI/Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Mystira API",
        Version = "v1",
        Description = "Backend API for Mystira - Dynamic Story App for Child Development",
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
        if (type == typeof(Mystira.App.Domain.Models.CharacterMetadata))
        {
            return "DomainCharacterMetadata";
        }

        if (type == typeof(Mystira.App.Api.Models.CharacterMetadata))
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
        options.UseCosmos(cosmosConnectionString!, "MystiraAppDb")
               .AddInterceptors(new PartitionKeyInterceptor()));
}
else
{
    // LOCAL DEVELOPMENT DATABASE: In-Memory for testing/development
    builder.Services.AddDbContext<MystiraAppDbContext>(options =>
        options.UseInMemoryDatabase("MystiraAppInMemoryDb_Local"));
}

// Register DbContext base type for repositories and UnitOfWork that depend on it
builder.Services.AddScoped<DbContext>(sp => sp.GetRequiredService<MystiraAppDbContext>());

// Add Azure Infrastructure Services
builder.Services.AddAzureBlobStorage(builder.Configuration);
builder.Services.Configure<AudioTranscodingOptions>(builder.Configuration.GetSection(AudioTranscodingOptions.SectionName));
// Register Application.Ports.Media.IAudioTranscodingService for use cases
builder.Services.AddSingleton<IAudioTranscodingService, FfmpegAudioTranscodingService>();

// Add Story Protocol Services
builder.Services.AddStoryProtocolServices(builder.Configuration);

// Configure JWT Authentication - Load from secure configuration only
var jwtIssuer = builder.Configuration["JwtSettings:Issuer"];
var jwtAudience = builder.Configuration["JwtSettings:Audience"];
var jwtRsaPublicKey = builder.Configuration["JwtSettings:RsaPublicKey"];
var jwtKey = builder.Configuration["JwtSettings:SecretKey"];
var jwksEndpoint = builder.Configuration["JwtSettings:JwksEndpoint"];

// Fail fast if JWT configuration is missing
if (string.IsNullOrWhiteSpace(jwtIssuer))
{
    throw new InvalidOperationException("JWT Issuer (JwtSettings:Issuer) is not configured.");
}

if (string.IsNullOrWhiteSpace(jwtAudience))
{
    throw new InvalidOperationException("JWT Audience (JwtSettings:Audience) is not configured.");
}

// Determine which signing method to use
bool useAsymmetric = !string.IsNullOrWhiteSpace(jwtRsaPublicKey) || !string.IsNullOrWhiteSpace(jwksEndpoint);
bool useSymmetric = !string.IsNullOrWhiteSpace(jwtKey);

if (!useAsymmetric && !useSymmetric)
{
    throw new InvalidOperationException(
        "JWT signing key not configured. Please provide either:\n" +
        "- JwtSettings:RsaPublicKey for asymmetric RS256 verification (recommended), OR\n" +
        "- JwtSettings:JwksEndpoint for JWKS-based key rotation (recommended), OR\n" +
        "- JwtSettings:SecretKey for symmetric HS256 verification (legacy)\n" +
        "Keys must be loaded from secure stores (Azure Key Vault, AWS Secrets Manager, etc.). " +
        "Never hardcode secrets in source code.");
}

builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = "Bearer";
        options.DefaultChallengeScheme = "Bearer";
        options.DefaultScheme = "Bearer";
    })
    .AddJwtBearer("Bearer", options =>
    {
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            ClockSkew = TimeSpan.FromMinutes(5),
            // Map JWT claim names to ClaimTypes for proper authorization
            // This allows simple claim names like "role" to work with [Authorize(Roles = "...")]
            RoleClaimType = "role",  // Map "role" claim to ClaimTypes.Role
            NameClaimType = "name"   // Map "name" claim to ClaimTypes.Name
        };

        if (!string.IsNullOrWhiteSpace(jwksEndpoint))
        {
            // Use JWKS endpoint for key rotation support (most secure)
            options.MetadataAddress = jwksEndpoint;
            options.RequireHttpsMetadata = true; // Always require HTTPS in production
            validationParameters.IssuerSigningKeyResolver = (token, securityToken, kid, validationParameters) =>
            {
                // This will automatically fetch keys from the JWKS endpoint
                using var client = new HttpClient();
                var response = client.GetStringAsync(jwksEndpoint).Result;
                var keys = new JsonWebKeySet(response);
                return keys.Keys;
            };
        }
        else if (!string.IsNullOrWhiteSpace(jwtRsaPublicKey))
        {
            // Use RSA public key for asymmetric verification (recommended)
            try
            {
                var rsa = System.Security.Cryptography.RSA.Create();
                rsa.ImportFromPem(jwtRsaPublicKey);
                validationParameters.IssuerSigningKey = new RsaSecurityKey(rsa);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "Failed to load RSA public key. Ensure JwtSettings:RsaPublicKey contains a valid PEM-encoded RSA public key " +
                    "from a secure store (Azure Key Vault, AWS Secrets Manager, etc.)", ex);
            }
        }
        else if (!string.IsNullOrWhiteSpace(jwtKey))
        {
            // Fall back to symmetric key (legacy - should be phased out)
            validationParameters.IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            // Log warning during startup (will be logged when app runs)
            Console.WriteLine("WARNING: Using symmetric HS256 JWT signing. Consider migrating to asymmetric RS256 with JWKS for better security.");
        }

        options.TokenValidationParameters = validationParameters;

        options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                // Skip bearer processing for auth routes so expired tokens don't block refresh/sign-in
                var path = context.HttpContext.Request.Path.Value ?? string.Empty;
                // List of route prefixes to skip; keep in sync with PWA interceptor
                // Include public, health-like endpoints where auth must not block request handling
                string[] skipPrefixes = new[]
                {
                    "/api/auth/refresh",
                    "/api/auth/signin",
                    "/api/auth/verify",
                    "/api/auth",
                    "/api/discord/status"
                };

                if (skipPrefixes.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
                {
                    // Skip bearer processing for public/auth routes
                    // Note: Using Debug level to avoid log spam from health check endpoints
                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                    logger.LogDebug("Skipping JWT bearer processing for auth route: {Path}", path);
                    context.NoResult();
                    return Task.CompletedTask;
                }

                return Task.CompletedTask;
            },
            OnAuthenticationFailed = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                var ua = context.HttpContext.Request.Headers["User-Agent"].ToString();
                var path = context.HttpContext.Request.Path;
                logger.LogError(context.Exception, "JWT authentication failed on {Path} (UA: {UserAgent})", path, ua);
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogInformation("JWT token validated for user: {User}", context.Principal?.Identity?.Name);
                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogWarning("JWT challenge on {Path}: {Error} - {Description}", context.HttpContext.Request.Path, context.Error, context.ErrorDescription);
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// Register repositories
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>)); // Generic repository
builder.Services.AddScoped<IGameSessionRepository, GameSessionRepository>();
builder.Services.AddScoped<IUserProfileRepository, UserProfileRepository>();
builder.Services.AddScoped<IAccountRepository, AccountRepository>();
builder.Services.AddScoped<IScenarioRepository, ScenarioRepository>();
builder.Services.AddScoped<ICharacterMapRepository, CharacterMapRepository>();
builder.Services.AddScoped<IContentBundleRepository, ContentBundleRepository>();
builder.Services.AddScoped<IBadgeConfigurationRepository, BadgeConfigurationRepository>();
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
builder.Services.AddScoped<IUnitOfWork, Mystira.App.Infrastructure.Data.UnitOfWork.UnitOfWork>();

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

// Account Use Cases
builder.Services.AddScoped<GetAccountByEmailUseCase>();
builder.Services.AddScoped<GetAccountUseCase>();
builder.Services.AddScoped<CreateAccountUseCase>();
builder.Services.AddScoped<UpdateAccountUseCase>();
builder.Services.AddScoped<AddUserProfileToAccountUseCase>();
builder.Services.AddScoped<RemoveUserProfileFromAccountUseCase>();
builder.Services.AddScoped<AddCompletedScenarioUseCase>();

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

// Register application services
builder.Services.AddScoped<IHealthCheckService, HealthCheckServiceAdapter>();
builder.Services.AddScoped<IAppStatusService, AppStatusService>();
builder.Services.AddScoped<IJwtService, JwtService>();

// Register Application.Ports adapters for CQRS handlers
builder.Services.AddScoped<Mystira.App.Application.Ports.Auth.IJwtService, JwtServiceAdapter>();
// Use infrastructure email service directly - configuration is read from AzureCommunicationServices section
builder.Services.AddAzureEmailService(builder.Configuration);
builder.Services.AddScoped<IHealthCheckPort, HealthCheckPortAdapter>();
builder.Services.AddScoped<IMediaMetadataService, MediaMetadataService>();

// Configure Memory Cache for query caching
builder.Services.AddMemoryCache(options =>
{
    options.SizeLimit = 1024; // Limit cache to 1024 entries
    options.CompactionPercentage = 0.25; // Compact 25% when size limit reached
});

// Configure MediatR for CQRS pattern
builder.Services.AddMediatR(cfg =>
{
    // Register all handlers from Application assembly
    cfg.RegisterServicesFromAssembly(typeof(Mystira.App.Application.CQRS.ICommand<>).Assembly);

    // Add query caching pipeline behavior
    cfg.AddOpenBehavior(typeof(QueryCachingBehavior<,>));
});

// Register query cache invalidation service
builder.Services.AddSingleton<IQueryCacheInvalidationService, QueryCacheInvalidationService>();

// Configure Health Checks
builder.Services.AddHealthChecks()
    .AddCheck<BlobStorageHealthCheck>("blob_storage");

// Add Discord Bot Integration (Optional - controlled by configuration)
var discordEnabled = builder.Configuration.GetValue<bool>("Discord:Enabled", false);
if (discordEnabled)
{
    builder.Services.AddDiscordBot(builder.Configuration);
    builder.Services.AddDiscordBotHostedService();
    builder.Services.AddHealthChecks()
        .AddDiscordBotHealthCheck();
}
else
{
    // Register No-Op implementations so MediatR handlers depending on chat bot ports still resolve
    builder.Services.AddSingleton<NoOpChatBotService>();
    builder.Services.AddSingleton<IChatBotService>(sp => sp.GetRequiredService<NoOpChatBotService>());
    builder.Services.AddSingleton<IMessagingService>(sp => sp.GetRequiredService<NoOpChatBotService>());
    builder.Services.AddSingleton<IBotCommandService>(sp => sp.GetRequiredService<NoOpChatBotService>());
}

// Configure CORS for frontend integration (Best Practices)
var policyName = "MystiraAppPolicy";
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
            // Fallback to default origins (development/local + production SWAs)
            originsToUse = new[]
            {
                "http://localhost:7000",
                "https://localhost:7000",
                "https://mystira.app",                                    // Production domain
                "https://blue-water-0eab7991e.3.azurestaticapps.net",    // Prod SWA
                "https://brave-meadow-0ecd87c03.3.azurestaticapps.net",  // Dev SWA (South Africa North)
                "https://dev-euw-swa-mystira-app.azurestaticapps.net",   // Dev SWA (West Europe - if custom domain)
                "https://dev-san-swa-mystira-app.azurestaticapps.net"    // Dev SWA (South Africa North - if custom domain)
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

// Configure Rate Limiting (BUG-5: Prevent brute-force attacks)
builder.Services.AddRateLimiter(options =>
{
    // Global rate limit: 100 requests per minute per IP
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(ipAddress, _ =>
            new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            });
    });

    // Strict rate limit for authentication endpoints: 5 attempts per 15 minutes per IP
    options.AddFixedWindowLimiter("auth", options =>
    {
        options.PermitLimit = 5;
        options.Window = TimeSpan.FromMinutes(15);
        options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        options.QueueLimit = 0;
    });

    // Rejection response
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await context.HttpContext.Response.WriteAsync(
            "Too many requests. Please try again later.",
            cancellationToken);
    };
});

var app = builder.Build();

var logger = app.Logger;
logger.LogInformation(useCosmosDb ? "Using Azure Cosmos DB (Cloud Database)" : "Using In-Memory Database (Local Development)");
logger.LogInformation(discordEnabled ? "Discord bot integration: ENABLED" : "Discord bot integration: DISABLED");

// Configure the HTTP request pipeline
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Mystira API v1");
    c.RoutePrefix = string.Empty; // Serve Swagger UI at root
});

// Only use HTTPS redirection in development
// In production (Azure App Service), HTTPS is handled at the load balancer level
if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// Add OWASP security headers (BUG-6)
app.UseSecurityHeaders();

// Add rate limiting (BUG-5)
app.UseRateLimiter();

app.UseRouting();

// ✅ CORS must be between UseRouting and auth/endpoints
app.UseCors(policyName);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

// Initialize database
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<MystiraAppDbContext>();
    try
    {
        await context.Database.EnsureCreatedAsync();
    }
    catch (Exception ex)
    {
        // Log error but continue - some environments may not have database access
        var startupLogger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        startupLogger.LogWarning(ex, "Failed to initialize database during startup. Continuing without database initialization.");
    }
}

app.Run();

// Make Program class accessible for testing
namespace Mystira.App.Api
{
    public partial class Program { }
}
