using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Mystira.App.Shared.Configuration;

/// <summary>
/// Extension methods for configuring Azure Key Vault as a configuration source.
/// Enables secure secret management with managed identity support.
/// </summary>
public static class KeyVaultConfigurationExtensions
{
    /// <summary>
    /// Adds Azure Key Vault as a configuration source using managed identity.
    /// Only activates in non-development environments or when explicitly configured.
    /// </summary>
    /// <param name="builder">The configuration builder</param>
    /// <param name="environment">The hosting environment</param>
    /// <returns>The configuration builder for chaining</returns>
    public static IConfigurationBuilder AddKeyVaultConfiguration(
        this IConfigurationBuilder builder,
        IHostEnvironment environment)
    {
        var config = builder.Build();
        var keyVaultName = config["KeyVault:Name"];

        // Skip Key Vault in development unless explicitly configured
        if (string.IsNullOrEmpty(keyVaultName))
        {
            return builder;
        }

        var keyVaultUri = new Uri($"https://{keyVaultName}.vault.azure.net/");

        // Use DefaultAzureCredential which supports:
        // - Managed Identity (production)
        // - Azure CLI / Visual Studio credentials (development)
        // - Environment variables (CI/CD)
        var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
        {
            // Exclude interactive credentials in production for security
            ExcludeInteractiveBrowserCredential = !environment.IsDevelopment(),
            ExcludeVisualStudioCodeCredential = !environment.IsDevelopment(),

            // Reduce timeout in production - fail fast if managed identity not available
            Retry =
            {
                MaxRetries = environment.IsDevelopment() ? 3 : 1,
                NetworkTimeout = TimeSpan.FromSeconds(environment.IsDevelopment() ? 10 : 3)
            }
        });

        builder.AddAzureKeyVault(keyVaultUri, credential);

        return builder;
    }

    /// <summary>
    /// Adds Azure Key Vault configuration to the host builder.
    /// </summary>
    /// <param name="hostBuilder">The host builder</param>
    /// <returns>The host builder for chaining</returns>
    public static IHostBuilder AddKeyVaultConfiguration(this IHostBuilder hostBuilder)
    {
        return hostBuilder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddKeyVaultConfiguration(context.HostingEnvironment);
        });
    }
}
