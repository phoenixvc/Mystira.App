using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mystira.App.Domain.Models;

namespace Mystira.App.Admin.Web.Controllers;

[Authorize]
[Route("admin")]
public class AdminController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        IHttpClientFactory httpClientFactory,
        ILogger<AdminController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    [AllowAnonymous]
    [HttpGet("login")]
    public IActionResult Login()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction(nameof(Dashboard));
        }

        return View("Login");
    }

    [HttpGet("")]
    public IActionResult Dashboard()
    {
        return View("Dashboard");
    }

    [HttpGet("scenarios")]
    public IActionResult Scenarios() => View("Scenarios");

    [HttpGet("badges")]
    public IActionResult Badges() => View("Badges");

    [HttpGet("badges/images")]
    public IActionResult BadgeImages() => View("BadgeImages");

    [HttpGet("media")]
    public IActionResult Media() => View("Media");

    [HttpGet("media-metadata")]
    public IActionResult MediaMetadata() => View("MediaMetadata");

    [HttpGet("character-media-metadata")]
    public IActionResult CharacterMediaMetadata() => View("CharacterMediaMetadata");

    [HttpGet("bundles")]
    public IActionResult Bundles() => View("Bundles");

    [HttpGet("avatars")]
    public IActionResult AvatarManagement() => View("AvatarManagement");

    [HttpGet("scenarios/import")]
    public IActionResult ImportScenario() => View("ImportScenario");

    [HttpGet("media/import")]
    public IActionResult ImportMedia() => View("ImportMedia");

    [HttpGet("bundles/import")]
    public IActionResult ImportBundle() => View("ImportBundle");

    [HttpGet("badges/import")]
    public IActionResult ImportBadges() => View("ImportBadges");

    [HttpGet("compassaxes")]
    public IActionResult CompassAxes() => View("CompassAxes");

    [HttpGet("archetypes")]
    public IActionResult Archetypes() => View("Archetypes");

    [HttpGet("agegroups")]
    public IActionResult AgeGroups() => View("AgeGroups");

    [HttpGet("charactermaps/import")]
    public IActionResult ImportCharacterMap() => View("ImportCharacterMap");

    [HttpGet("scenarios/edit/{id}")]
    public async Task<IActionResult> EditScenario(string id)
    {
        try
        {
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var client = _httpClientFactory.CreateClient();
            var scenario = await client.GetFromJsonAsync<Scenario>($"{baseUrl}/api/scenarios/{id}");

            if (scenario == null)
            {
                return NotFound();
            }

            return View("EditScenario", scenario);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading scenario for editing: {ScenarioId}", id);
            return StatusCode(500, "Error loading scenario");
        }
    }

    [AllowAnonymous]
    [HttpGet("forbidden")]
    public IActionResult Forbidden()
    {
        return StatusCode(StatusCodes.Status403Forbidden);
    }

    [AllowAnonymous]
    [HttpGet("error")]
    public IActionResult Error()
    {
        return StatusCode(StatusCodes.Status500InternalServerError);
    }
}
