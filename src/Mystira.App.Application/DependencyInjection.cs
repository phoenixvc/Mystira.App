using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Mystira.App.Application;

/// <summary>
/// Extension methods for registering Application layer services.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers all Application layer services including validators.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Register all validators from this assembly
        services.AddValidatorsFromAssemblyContaining<Validators.StartGameSessionCommandValidator>();

        return services;
    }
}
