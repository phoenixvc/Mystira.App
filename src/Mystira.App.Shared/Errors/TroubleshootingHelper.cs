namespace Mystira.App.Shared.Errors;

/// <summary>
/// Provides troubleshooting suggestions for common errors.
/// Maps error patterns to actionable solutions users can follow.
/// </summary>
public static class TroubleshootingHelper
{
    private static readonly List<ErrorPattern> _errorPatterns = new()
    {
        // Azure Location Errors
        new ErrorPattern
        {
            Code = "AZURE_LOCATION_001",
            Pattern = "LocationNotAvailableForResourceType",
            Title = "Azure Region Not Available for Resource Type",
            Category = ErrorCategory.Azure,
            Description = "The selected Azure region doesn't support this resource type.",
            Solutions = new[]
            {
                "Check available regions for the resource type using: az provider show --namespace Microsoft.Web --query \"resourceTypes[?resourceType=='staticSites'].locations\"",
                "Common available regions for Static Web Apps: westus2, centralus, eastus2, westeurope, eastasia",
                "Update your deployment script to use a supported region",
                "Consider using a different resource type that's available in your preferred region"
            },
            RelatedLinks = new[]
            {
                ("Azure region availability", "https://azure.microsoft.com/en-us/explore/global-infrastructure/products-by-region/"),
                ("Static Web Apps regions", "https://docs.microsoft.com/azure/static-web-apps/overview#regional-availability")
            }
        },

        new ErrorPattern
        {
            Code = "AZURE_LOCATION_002",
            Pattern = "InvalidResourceGroupLocation",
            Title = "Resource Group Location Conflict",
            Category = ErrorCategory.Azure,
            Description = "The resource group already exists in a different location.",
            Solutions = new[]
            {
                "Option 1: Use the existing resource group's location",
                "Option 2: Delete the existing resource group first: az group delete --name <rg-name> --yes --no-wait",
                "Option 3: Use a different resource group name for the new location",
                "Note: Wait 1-2 minutes after deletion before creating a new group"
            },
            RelatedLinks = new[]
            {
                ("Resource groups", "https://docs.microsoft.com/azure/azure-resource-manager/management/manage-resource-groups-portal")
            }
        },

        // Authentication Errors
        new ErrorPattern
        {
            Code = "AUTH_001",
            Pattern = "AADSTS\\d+",
            Title = "Azure AD Authentication Error",
            Category = ErrorCategory.Authentication,
            Description = "There was a problem with Azure Active Directory authentication.",
            Solutions = new[]
            {
                "Run 'az login' to refresh your authentication",
                "If using a service principal, verify the client secret hasn't expired",
                "Check that your Azure subscription is active",
                "For device login: use 'az login --use-device-code'"
            },
            RelatedLinks = new[]
            {
                ("Azure CLI login", "https://docs.microsoft.com/cli/azure/authenticate-azure-cli")
            }
        },

        new ErrorPattern
        {
            Code = "AUTH_002",
            Pattern = "UnauthorizedError|401|Unauthorized",
            Title = "Unauthorized Access",
            Category = ErrorCategory.Authentication,
            Description = "You don't have permission to access this resource.",
            Solutions = new[]
            {
                "Verify your login credentials are correct",
                "Check if your session has expired (try logging in again)",
                "Ensure you have the required role assignments in Azure",
                "For APIs: verify your API key or JWT token is valid"
            }
        },

        // Database Errors
        new ErrorPattern
        {
            Code = "DB_001",
            Pattern = "CosmosException|Request rate is large",
            Title = "Cosmos DB Rate Limiting",
            Category = ErrorCategory.Database,
            Description = "Too many requests to the database.",
            Solutions = new[]
            {
                "Wait a few seconds and retry the operation",
                "Consider increasing provisioned throughput (RU/s)",
                "Implement exponential backoff in your retry logic",
                "Review queries for optimization opportunities"
            },
            RelatedLinks = new[]
            {
                ("Cosmos DB RU/s", "https://docs.microsoft.com/azure/cosmos-db/request-units")
            }
        },

        new ErrorPattern
        {
            Code = "DB_002",
            Pattern = "PartitionKey|partition key",
            Title = "Partition Key Error",
            Category = ErrorCategory.Database,
            Description = "There's an issue with the Cosmos DB partition key.",
            Solutions = new[]
            {
                "Ensure all documents include the partition key field",
                "Verify the partition key path matches your container configuration",
                "Check that partition key values are not null or empty",
                "Review your entity model for proper PartitionKey attribute"
            }
        },

        // Network Errors
        new ErrorPattern
        {
            Code = "NET_001",
            Pattern = "ECONNREFUSED|Connection refused",
            Title = "Connection Refused",
            Category = ErrorCategory.Network,
            Description = "Unable to connect to the service.",
            Solutions = new[]
            {
                "Verify the service is running and listening on the expected port",
                "Check firewall rules and network security groups",
                "Ensure the URL/hostname is correct",
                "For local development: verify the API/service has started successfully"
            }
        },

        new ErrorPattern
        {
            Code = "NET_002",
            Pattern = "ETIMEDOUT|timeout|timed out",
            Title = "Connection Timeout",
            Category = ErrorCategory.Network,
            Description = "The connection took too long to respond.",
            Solutions = new[]
            {
                "Check your internet connection",
                "The server might be overloaded - try again in a few minutes",
                "Increase timeout settings if available",
                "Check if any VPN or proxy is affecting the connection"
            }
        },

        // Configuration Errors
        new ErrorPattern
        {
            Code = "CONFIG_001",
            Pattern = "Configuration value.*not found|Missing configuration",
            Title = "Missing Configuration",
            Category = ErrorCategory.Configuration,
            Description = "A required configuration value is missing.",
            Solutions = new[]
            {
                "Check appsettings.json for the missing value",
                "For local development: ensure appsettings.Development.json exists",
                "For secrets: use 'dotnet user-secrets set <key> <value>'",
                "For Azure: verify App Configuration or environment variables are set"
            }
        },

        new ErrorPattern
        {
            Code = "CONFIG_002",
            Pattern = "InvalidOperationException.*JWT|JwtSettings",
            Title = "JWT Configuration Error",
            Category = ErrorCategory.Configuration,
            Description = "JWT authentication is not configured correctly.",
            Solutions = new[]
            {
                "Verify JwtSettings section in appsettings.json",
                "Ensure SecretKey is at least 32 characters",
                "Check Issuer and Audience values match your application",
                "For production: use environment variables or Azure Key Vault"
            }
        },

        // Build/Deployment Errors
        new ErrorPattern
        {
            Code = "BUILD_001",
            Pattern = "npm ERR!|node_modules",
            Title = "NPM Installation Error",
            Category = ErrorCategory.Build,
            Description = "There was a problem installing npm packages.",
            Solutions = new[]
            {
                "Delete node_modules folder and package-lock.json, then run 'npm install' again",
                "Check you have the correct Node.js version installed (see package.json engines)",
                "Clear npm cache: npm cache clean --force",
                "Check for network issues or corporate proxy settings"
            }
        },

        new ErrorPattern
        {
            Code = "BUILD_002",
            Pattern = "Rust|cargo|tauri",
            Title = "Rust/Tauri Build Error",
            Category = ErrorCategory.Build,
            Description = "There was a problem building the Tauri application.",
            Solutions = new[]
            {
                "Ensure Rust is installed: curl --proto '=https' --tlsv1.2 -sSf https://sh.rustup.rs | sh",
                "Update Rust toolchain: rustup update",
                "Install Tauri dependencies for your OS (see Tauri prerequisites)",
                "Try cleaning the build: cargo clean"
            },
            RelatedLinks = new[]
            {
                ("Tauri Prerequisites", "https://tauri.app/v1/guides/getting-started/prerequisites")
            }
        },

        new ErrorPattern
        {
            Code = "BUILD_003",
            Pattern = "dotnet.*restore|NuGet",
            Title = ".NET Package Restore Error",
            Category = ErrorCategory.Build,
            Description = "There was a problem restoring .NET packages.",
            Solutions = new[]
            {
                "Check your internet connection",
                "Verify NuGet.config has the correct package sources",
                "Clear NuGet cache: dotnet nuget locals all --clear",
                "Try restoring with verbose logging: dotnet restore --verbosity detailed"
            }
        },

        // Generic/Fallback
        new ErrorPattern
        {
            Code = "GEN_001",
            Pattern = "Exception|Error|Failed",
            Title = "Unexpected Error",
            Category = ErrorCategory.Unknown,
            Description = "An unexpected error occurred.",
            Solutions = new[]
            {
                "Check the error message and stack trace for more details",
                "Review application logs for additional context",
                "Try restarting the application or service",
                "If the problem persists, report the issue with the full error details"
            }
        }
    };

    /// <summary>
    /// Analyzes an error message and returns troubleshooting suggestions.
    /// </summary>
    public static TroubleshootingResult Analyze(string errorMessage, Exception? exception = null)
    {
        var fullText = exception != null
            ? $"{errorMessage} {exception.Message} {exception.GetType().Name}"
            : errorMessage;

        foreach (var pattern in _errorPatterns)
        {
            if (System.Text.RegularExpressions.Regex.IsMatch(fullText, pattern.Pattern,
                System.Text.RegularExpressions.RegexOptions.IgnoreCase))
            {
                return new TroubleshootingResult
                {
                    Matched = true,
                    ErrorCode = pattern.Code,
                    Title = pattern.Title,
                    Category = pattern.Category,
                    Description = pattern.Description,
                    OriginalError = errorMessage,
                    Solutions = pattern.Solutions.ToList(),
                    RelatedLinks = pattern.RelatedLinks?.ToList() ?? new()
                };
            }
        }

        // Return generic guidance if no pattern matched
        return new TroubleshootingResult
        {
            Matched = false,
            ErrorCode = "UNKNOWN",
            Title = "Unknown Error",
            Category = ErrorCategory.Unknown,
            Description = "We couldn't identify this specific error pattern.",
            OriginalError = errorMessage,
            Solutions = new List<string>
            {
                "Check the error message carefully for clues",
                "Search for this error message online",
                "Review recent changes that might have caused this",
                "Ask for help with the full error message and context"
            }
        };
    }

    /// <summary>
    /// Gets all known error patterns for documentation purposes.
    /// </summary>
    public static IReadOnlyList<ErrorPattern> GetAllPatterns() => _errorPatterns.AsReadOnly();

    /// <summary>
    /// Gets error patterns by category.
    /// </summary>
    public static IEnumerable<ErrorPattern> GetPatternsByCategory(ErrorCategory category)
        => _errorPatterns.Where(p => p.Category == category);
}

public enum ErrorCategory
{
    Azure,
    Authentication,
    Database,
    Network,
    Configuration,
    Build,
    Unknown
}

public class ErrorPattern
{
    public required string Code { get; init; }
    public required string Pattern { get; init; }
    public required string Title { get; init; }
    public required ErrorCategory Category { get; init; }
    public required string Description { get; init; }
    public required string[] Solutions { get; init; }
    public (string Title, string Url)[]? RelatedLinks { get; init; }
}

public class TroubleshootingResult
{
    public bool Matched { get; init; }
    public required string ErrorCode { get; init; }
    public required string Title { get; init; }
    public required ErrorCategory Category { get; init; }
    public required string Description { get; init; }
    public required string OriginalError { get; init; }
    public required List<string> Solutions { get; init; }
    public List<(string Title, string Url)> RelatedLinks { get; init; } = new();

    /// <summary>
    /// Formats the result as a user-friendly string.
    /// </summary>
    public string ToDisplayString()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"═══════════════════════════════════════════════════════════════");
        sb.AppendLine($"  Error: {Title} [{ErrorCode}]");
        sb.AppendLine($"═══════════════════════════════════════════════════════════════");
        sb.AppendLine();
        sb.AppendLine($"Category: {Category}");
        sb.AppendLine($"Description: {Description}");
        sb.AppendLine();
        sb.AppendLine("Suggested Solutions:");
        for (int i = 0; i < Solutions.Count; i++)
        {
            sb.AppendLine($"  {i + 1}. {Solutions[i]}");
        }

        if (RelatedLinks.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("Related Resources:");
            foreach (var (title, url) in RelatedLinks)
            {
                sb.AppendLine($"  • {title}: {url}");
            }
        }

        return sb.ToString();
    }
}
