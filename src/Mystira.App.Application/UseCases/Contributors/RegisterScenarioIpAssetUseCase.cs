using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports;
using Mystira.App.Application.Ports.Data;
using Mystira.Contracts.App.Requests.Contributors;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.UseCases.Contributors;

/// <summary>
/// Use case for registering a scenario as an IP Asset on Story Protocol
/// </summary>
public class RegisterScenarioIpAssetUseCase
{
    private readonly IScenarioRepository _scenarioRepository;
    private readonly IStoryProtocolService _storyProtocolService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RegisterScenarioIpAssetUseCase> _logger;

    public RegisterScenarioIpAssetUseCase(
        IScenarioRepository scenarioRepository,
        IStoryProtocolService storyProtocolService,
        IUnitOfWork unitOfWork,
        ILogger<RegisterScenarioIpAssetUseCase> logger)
    {
        _scenarioRepository = scenarioRepository;
        _storyProtocolService = storyProtocolService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<StoryProtocolMetadata> ExecuteAsync(string scenarioId, RegisterIpAssetRequest request)
    {
        // Get the scenario
        var scenario = await _scenarioRepository.GetByIdAsync(scenarioId);
        if (scenario == null)
        {
            throw new ArgumentException($"Scenario not found: {scenarioId}");
        }

        // Check if already registered
        if (scenario.StoryProtocol?.IsRegistered ?? false)
        {
            throw new InvalidOperationException($"Scenario {scenarioId} is already registered on Story Protocol");
        }

        // Ensure contributors are set
        if (scenario.StoryProtocol == null || !scenario.StoryProtocol.Contributors.Any())
        {
            throw new InvalidOperationException("Contributors must be set before registering on Story Protocol");
        }

        // Validate contributor splits
        if (!scenario.StoryProtocol.ValidateContributorSplits(out var errors))
        {
            var errorMessage = string.Join("; ", errors);
            throw new ArgumentException($"Invalid contributor configuration: {errorMessage}");
        }

        // Register on Story Protocol
        var storyProtocolMetadata = await _storyProtocolService.RegisterIpAssetAsync(
            scenario.Id,
            scenario.Title,
            scenario.StoryProtocol.Contributors,
            request.MetadataUri,
            request.LicenseTermsId);

        // Update the scenario with Story Protocol metadata
        scenario.StoryProtocol = storyProtocolMetadata;

        await _scenarioRepository.UpdateAsync(scenario);

        try
        {
            await _unitOfWork.SaveChangesAsync();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error saving Story Protocol registration for scenario: {ScenarioId}", scenarioId);
            throw;
        }

        _logger.LogInformation("Registered scenario {ScenarioId} as IP Asset: {IpAssetId}",
            scenarioId, storyProtocolMetadata.IpAssetId);

        return storyProtocolMetadata;
    }
}
