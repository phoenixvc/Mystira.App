using FluentValidation;
using Mystira.App.Application.CQRS.GameSessions.Commands;

namespace Mystira.App.Application.Validators;

/// <summary>
/// Validates StartGameSessionCommand before processing.
/// </summary>
public class StartGameSessionCommandValidator : AbstractValidator<StartGameSessionCommand>
{
    public StartGameSessionCommandValidator()
    {
        RuleFor(x => x.Request)
            .NotNull()
            .WithMessage("Game session request is required");

        RuleFor(x => x.Request.ScenarioId)
            .NotEmpty()
            .WithMessage("Scenario ID is required")
            .When(x => x.Request != null);

        RuleFor(x => x.Request.UserProfileId)
            .NotEmpty()
            .WithMessage("User profile ID is required")
            .When(x => x.Request != null);
    }
}
