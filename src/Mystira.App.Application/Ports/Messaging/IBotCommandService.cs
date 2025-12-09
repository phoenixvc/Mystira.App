using System.Reflection;

namespace Mystira.App.Application.Ports.Messaging;

/// <summary>
/// Port interface for bot command (slash command/interaction) operations.
/// Platform-agnostic interface for registering and managing bot commands.
/// Implementations handle platform-specific command registration (Discord slash commands, Teams commands, etc.).
/// </summary>
public interface IBotCommandService
{
    /// <summary>
    /// Register command modules from the specified assembly.
    /// </summary>
    /// <param name="assembly">Assembly containing command modules</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RegisterCommandsAsync(Assembly assembly, CancellationToken cancellationToken = default);

    /// <summary>
    /// Register commands to a specific server/guild (faster for development).
    /// </summary>
    /// <param name="serverId">Target server/guild ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RegisterCommandsToServerAsync(ulong serverId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Register commands globally (may take time to propagate depending on platform).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RegisterCommandsGloballyAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Whether commands are enabled.
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Number of registered command modules.
    /// </summary>
    int RegisteredModuleCount { get; }
}

/// <summary>
/// Status information for bot command service (platform-agnostic).
/// </summary>
public class BotCommandStatus
{
    public bool IsEnabled { get; set; }
    public int ModuleCount { get; set; }
    public int CommandCount { get; set; }
    /// <summary>
    /// Server/guild/workspace ID where commands are registered
    /// </summary>
    public ulong? ServerId { get; set; }
    public bool IsGloballyRegistered { get; set; }
}
