using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;

namespace Mystira.App.Shared.Telemetry;

/// <summary>
/// Telemetry initializer that sets the cloud role name for Application Insights.
/// This helps identify which service generated the telemetry in distributed tracing scenarios.
/// </summary>
public class CloudRoleNameInitializer : ITelemetryInitializer
{
    private readonly string _roleName;
    private readonly string _environment;

    /// <summary>
    /// Creates a new CloudRoleNameInitializer.
    /// </summary>
    /// <param name="roleName">The name of the service (e.g., "Mystira.App.Api")</param>
    /// <param name="environment">The environment name (e.g., "Development", "Production")</param>
    public CloudRoleNameInitializer(string roleName, string environment)
    {
        _roleName = roleName ?? throw new ArgumentNullException(nameof(roleName));
        _environment = environment ?? "Unknown";
    }

    /// <summary>
    /// Initializes the telemetry item with cloud role information.
    /// </summary>
    public void Initialize(ITelemetry telemetry)
    {
        if (telemetry?.Context?.Cloud == null)
            return;

        // Set the cloud role name - this appears in Application Insights Application Map
        telemetry.Context.Cloud.RoleName = _roleName;

        // Set the role instance to include environment for easier filtering
        if (string.IsNullOrEmpty(telemetry.Context.Cloud.RoleInstance))
        {
            telemetry.Context.Cloud.RoleInstance = $"{_roleName}-{_environment}";
        }

        // Add environment as a custom property for filtering
        if (telemetry.Context.GlobalProperties != null &&
            !telemetry.Context.GlobalProperties.ContainsKey("Environment"))
        {
            telemetry.Context.GlobalProperties["Environment"] = _environment;
        }
    }
}
