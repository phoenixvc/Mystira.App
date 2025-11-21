using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Mystira.App.Api.Adapters;
using Mystira.App.Api.Data;
using Mystira.App.Api.Services;
using Mystira.App.Infrastructure.Azure;
using Mystira.App.Infrastructure.Azure.HealthChecks;
using Mystira.App.Infrastructure.Azure.Services;
using Mystira.App.Infrastructure.Data.Repositories;
using Mystira.App.Infrastructure.Data.UnitOfWork;

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
        options.UseCosmos(cosmosConnectionString!, "MystiraAppDb"));
}
else
{
    // LOCAL DEVELOPMENT DATABASE: In-Memory for testing/development
    builder.Services.AddDbContext<MystiraAppDbContext>(options =>
        options.UseInMemoryDatabase("MystiraAppInMemoryDb_Local"));
}

// Add Azure Infrastructure Services
builder.Services.AddAzureBlobStorage(builder.Configuration);
builder.Services.Configure<AudioTranscodingOptions>(builder.Configuration.GetSection(AudioTranscodingOptions.SectionName));
builder.Services.AddSingleton<IAudioTranscodingService, FfmpegAudioTranscodingService>();

// Register Content Bundle services
builder.Services.AddScoped<IContentBundleService, ContentBundleService>();

// Configure JWT Authentication - Load from secure configuration only
var jwtIssuer = builder.Configuration["JwtSettings:Issuer"];
var jwtAudience = builder.Configuration["JwtSettings:Audience"];
var jwtRsaPublicKey = builder.Configuration["JwtSettings:RsaPublicKey"];
var jwtKey = builder.Configuration["JwtSettings:SecretKey"];
var jwksEndpoint = builder.Configuration["JwtSettings:JwksEndpoint"];

// Fail fast if JWT configuration is missing
if (string.IsNullOrEmpty(jwtIssuer))
{
    throw new InvalidOperationException("JWT Issuer (JwtSettings:Issuer) is not configured.");
}

if (string.IsNullOrEmpty(jwtAudience))
{
    throw new InvalidOperationException("JWT Audience (JwtSettings:Audience) is not configured.");
}

// Determine which signing method to use
bool useAsymmetric = !string.IsNullOrEmpty(jwtRsaPublicKey) || !string.IsNullOrEmpty(jwksEndpoint);
bool useSymmetric = !string.IsNullOrEmpty(jwtKey);

if (!useAsymmetric && !useSymmetric)
{
    throw new InvalidOperationException(
        "JWT signing key not configured. Please provide either:\n" +
        "- JwtSettings:RsaPublicKey for asymmetric RS256 verification (recommended), OR\n" +
        "- JwtSettings:JwksEndpoint for JWKS-based key rotation (recommended), OR\n" +
        "- JwtSettings:Key for symmetric HS256 verification (legacy)\n" +
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
            ClockSkew = TimeSpan.FromMinutes(5)
        };

        if (!string.IsNullOrEmpty(jwksEndpoint))
        {
            // Use JWKS endpoint for key rotation support (most secure)
            options.MetadataAddress = jwksEndpoint;
            options.RequireHttpsMetadata = true; // Always require HTTPS in production
            validationParameters.IssuerSigningKeyResolver = (token, securityToken, kid, validationParameters) =>
            {
                // This will automatically fetch keys from the JWKS endpoint
                var client = new HttpClient();
                var response = client.GetStringAsync(jwksEndpoint).Result;
                var keys = new JsonWebKeySet(response);
                return keys.Keys;
            };
        }
        else if (!string.IsNullOrEmpty(jwtRsaPublicKey))
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
                    "Failed to load RSA public key. Ensure Jwt:RsaPublicKey contains a valid PEM-encoded RSA public key " +
                    "from a secure store (Azure Key Vault, AWS Secrets Manager, etc.)", ex);
            }
        }
        else if (!string.IsNullOrEmpty(jwtKey))
        {
            // Fall back to symmetric key (legacy - should be phased out)
            validationParameters.IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            // Log warning during startup (will be logged when app runs)
            Console.WriteLine("WARNING: Using symmetric HS256 JWT signing. Consider migrating to asymmetric RS256 with JWKS for better security.");
        }

        options.TokenValidationParameters = validationParameters;
        
        options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogError(context.Exception, "JWT authentication failed");
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogInformation("JWT token validated for user: {User}", context.Principal?.Identity?.Name);
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// Register repositories
builder.Services.AddScoped<IRepository<Mystira.App.Domain.Models.GameSession>, Repository<Mystira.App.Domain.Models.GameSession>>();
builder.Services.AddScoped<IGameSessionRepository, GameSessionRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Register application services
builder.Services.AddScoped<IScenarioApiService, ScenarioApiService>();
builder.Services.AddScoped<IGameSessionApiService, GameSessionApiService>();
builder.Services.AddScoped<IUserProfileApiService, UserProfileApiService>();
builder.Services.AddScoped<IUserBadgeApiService, UserBadgeApiService>();
builder.Services.AddScoped<IAccountApiService, AccountApiService>();
builder.Services.AddScoped<ICharacterMapApiService, CharacterMapApiService>();
builder.Services.AddScoped<IBadgeConfigurationApiService, BadgeConfigurationApiService>();
builder.Services.AddScoped<IHealthCheckService, HealthCheckServiceAdapter>();
builder.Services.AddScoped<IClientApiService, ClientApiService>();
builder.Services.AddScoped<IAppStatusService, AppStatusService>();
builder.Services.AddScoped<IMediaApiService, MediaApiService>();
builder.Services.AddScoped<IMediaMetadataService, MediaMetadataService>();
builder.Services.AddScoped<ICharacterMediaMetadataService, CharacterMediaMetadataService>();
builder.Services.AddScoped<IBundleService, BundleService>();
builder.Services.AddScoped<ICharacterMapFileService, CharacterMapFileService>();
builder.Services.AddScoped<IAvatarApiService, AvatarApiService>();
builder.Services.AddScoped<IPasswordlessAuthService, PasswordlessAuthService>();
builder.Services.AddScoped<IEmailService, AzureEmailService>();
builder.Services.AddScoped<IJwtService, JwtService>();

// Configure Health Checks
builder.Services.AddHealthChecks()
    .AddCheck<BlobStorageHealthCheck>("blob_storage");

// Configure CORS for frontend integration
var policyName = "MystiraAppPolicy";
builder.Services.AddCors(options =>
{
    options.AddPolicy(policyName, policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("CorsSettings:AllowedOrigins").Get<string>()?.Split(',') ?? Array.Empty<string>();
        if (allowedOrigins.Length == 0)
        {
            // Fallback to default origins if configuration is not set
            policy.WithOrigins(
                "http://localhost:7000",
                "https://localhost:7000",
                "https://mystiraapp.azurewebsites.net",
                "https://mystira.app");
        }
        else
        {
            policy.WithOrigins(allowedOrigins);
        }
        policy.AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Configure logging
builder.Logging.AddConsole();
builder.Logging.AddAzureWebAppDiagnostics();

var app = builder.Build();

var logger = app.Logger;
logger.LogInformation(useCosmosDb ? "Using Azure Cosmos DB (Cloud Database)" : "Using In-Memory Database (Local Development)");

// Configure the HTTP request pipeline
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Mystira API v1");
    c.RoutePrefix = string.Empty; // Serve Swagger UI at root
});

app.UseHttpsRedirection();

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
