using MediatR;
using Microsoft.Extensions.Logging;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.UserProfiles.Commands;

/// <summary>
/// Handler for creating multiple user profiles in a batch operation.
/// Delegates to CreateUserProfileCommand for each individual profile.
/// Used during onboarding when creating profiles for family members.
/// </summary>
public class CreateMultipleProfilesCommandHandler
    : ICommandHandler<CreateMultipleProfilesCommand, List<UserProfile>>
{
    private readonly IMediator _mediator;
    private readonly ILogger<CreateMultipleProfilesCommandHandler> _logger;

    public CreateMultipleProfilesCommandHandler(
        IMediator mediator,
        ILogger<CreateMultipleProfilesCommandHandler> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<List<UserProfile>> Handle(
        CreateMultipleProfilesCommand command,
        CancellationToken cancellationToken)
    {
        var createdProfiles = new List<UserProfile>();

        foreach (var profileRequest in command.Request.Profiles)
        {
            try
            {
                var createCommand = new CreateUserProfileCommand(profileRequest);
                var profile = await _mediator.Send(createCommand, cancellationToken);
                createdProfiles.Add(profile);

                _logger.LogInformation("Created profile {ProfileId} with name {Name} in batch",
                    profile.Id, profile.Name);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to create profile {Name} in batch", profileRequest.Name);
                // Continue with other profiles - partial success is acceptable
            }
        }

        _logger.LogInformation("Created {Count} of {Total} profiles in batch",
            createdProfiles.Count, command.Request.Profiles.Count);

        return createdProfiles;
    }
}
