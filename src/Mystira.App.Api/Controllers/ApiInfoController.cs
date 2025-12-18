using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Mystira.App.Shared.Models;

namespace Mystira.App.Api.Controllers;

/// <summary>
/// Provides API version and compatibility information.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ApiInfoController : ControllerBase
{
    /// <summary>
    /// Gets the current API version and compatibility information.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiVersionInfo), StatusCodes.Status200OK)]
    public ActionResult<ApiVersionInfo> GetApiInfo()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var assemblyVersion = assembly.GetName().Version;
        var version = assemblyVersion?.ToString() ?? "1.0.0";

        // If it's a dev build (often 1.0.0.0), try to provide more context
        if (version == "1.0.0.0" || version == "1.0.0")
        {
            version = "1.0.0-dev";
        }

        return Ok(new ApiVersionInfo
        {
            ApiVersion = "v1",
            BuildVersion = version,
            MasterDataVersion = "2.0.0", // DB-backed master data version
            SupportsLegacyEnums = true, // Backward compatibility for enum-based values
            MasterDataEntities = new[]
            {
                new MasterDataEntityInfo { Name = "CompassAxis", Endpoint = "/api/compassaxes", Count = null },
                new MasterDataEntityInfo { Name = "Archetype", Endpoint = "/api/archetypes", Count = null },
                new MasterDataEntityInfo { Name = "EchoType", Endpoint = "/api/echotypes", Count = null },
                new MasterDataEntityInfo { Name = "FantasyTheme", Endpoint = "/api/fantasythemes", Count = null },
                new MasterDataEntityInfo { Name = "AgeGroup", Endpoint = "/api/agegroups", Count = null }
            },
            DeprecatedApis = new[]
            {
                new DeprecatedApiInfo
                {
                    Endpoint = "Legacy JSON files",
                    ReplacedBy = "DB-backed master data APIs",
                    DeprecationDate = "2024-12-01",
                    RemovalDate = "2025-06-01"
                }
            }
        });
    }

    /// <summary>
    /// Gets backward compatibility mapping for legacy enum values.
    /// </summary>
    [HttpGet("legacy-mappings")]
    [ProducesResponseType(typeof(LegacyMappings), StatusCodes.Status200OK)]
    public ActionResult<LegacyMappings> GetLegacyMappings()
    {
        return Ok(new LegacyMappings
        {
            Message = "Use the DB-backed APIs for current values. Legacy enum values are still accepted for backward compatibility.",
            CoreAxisMappings = new Dictionary<string, string>
            {
                { "bravery_vs_caution", "Bravery_vs_Caution" },
                { "honesty_vs_deception", "Honesty_vs_Deception" },
                { "compassion_vs_logic", "Compassion_vs_Logic" },
                { "loyalty_vs_independence", "Loyalty_vs_Independence" }
            },
            ArchetypeMappings = new Dictionary<string, string>
            {
                { "rule_checker", "Rule Checker" },
                { "what_if_scientist", "What-If Scientist" },
                { "try_again_hero", "Try Again Hero" },
                { "tidy_expert", "Tidy Expert" },
                { "helper_captain_coop", "Helper Captain Coop" },
                { "rhythm_explorer", "Rhythm Explorer" }
            }
        });
    }
}
