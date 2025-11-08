using System.Text;
using Mystira.App.Api.Adapters;
using Mystira.App.Api.Data;
using Mystira.App.Api.Services;
using Mystira.App.Infrastructure.Azure;
using Mystira.App.Infrastructure.Azure.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

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
            return "DomainCharacterMetadata";
        if (type == typeof(Mystira.App.Api.Models.CharacterMetadata))
            return "ApiCharacterMetadata";
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

// Configure JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"] ?? "Mystira-app-Development-Secret-Key-2024-Very-Long-For-Security";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "mystira-app-api";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "mystira-app";

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = "Cookies";
        options.DefaultSignInScheme = "Cookies";
        options.DefaultChallengeScheme = "Cookies";
    })
    .AddCookie("Cookies", options =>
    {
        options.Cookie.Name = "Mystira.App.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
        options.LoginPath = "/api/auth/login";
        options.LogoutPath = "/api/auth/logout";
        options.AccessDeniedPath = "/api/auth/forbidden";
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

// Configure Health Checks
builder.Services.AddHealthChecks()
    .AddCheck<BlobStorageHealthCheck>("blob_storage");

// Configure CORS for frontend integration
builder.Services.AddCors(options =>
{
    options.AddPolicy("MystiraAppPolicy", policy =>
    {
        policy.WithOrigins(
                "http://localhost:7000",
                "https://localhost:7000",
                "https://mystiraapp.azurewebsites.net", 
                "https://mystira.app")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .SetIsOriginAllowedToAllowWildcardSubdomains()
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
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Mystira API v1");
        c.RoutePrefix = string.Empty; // Serve Swagger UI at root
    });
}

app.UseHttpsRedirection();
app.UseCors("MystiraAppPolicy");

app.UseRouting();
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
