using System.Reflection;

namespace Mystira.App.Application.Ports.Messaging;

/// <summary>
/// Port interface for slash command (interaction) operations.
/// Implementations handle platform-specific slash command registration and execution.
/// </summary>
public interface ISlashCommandService
{
    /// <summary>
    /// Register slash command modules from the specified assembly.
    /// </summary>
    /// <param name="assembly">Assembly containing command modules</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RegisterCommandsAsync(Assembly assembly, CancellationToken cancellationToken = default);

    /// <summary>
    /// Register slash commands to a specific guild (faster for development).
    /// </summary>
    /// <param name="guildId">Target guild ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RegisterCommandsToGuildAsync(ulong guildId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Register slash commands globally (takes up to 1 hour to propagate).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RegisterCommandsGloballyAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Whether slash commands are enabled.
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Number of registered command modules.
    /// </summary>
    int RegisteredModuleCount { get; }
}

/// <summary>
/// Status information for slash command service.
/// </summary>
public class SlashCommandStatus
{
    public bool IsEnabled { get; set; }
    public int ModuleCount { get; set; }
    public int CommandCount { get; set; }
    public ulong? GuildId { get; set; }
    public bool IsGloballyRegistered { get; set; }
}
