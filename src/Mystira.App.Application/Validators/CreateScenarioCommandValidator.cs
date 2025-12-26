using FluentValidation;
using Mystira.App.Application.CQRS.Scenarios.Commands;

namespace Mystira.App.Application.Validators;

/// <summary>
/// Validates CreateScenarioCommand before processing.
/// </summary>
public class CreateScenarioCommandValidator : AbstractValidator<CreateScenarioCommand>
{
    public CreateScenarioCommandValidator()
    {
        RuleFor(x => x.Request)
            .NotNull()
            .WithMessage("Scenario request is required");

        RuleFor(x => x.Request.Title)
            .NotEmpty()
            .WithMessage("Title is required")
            .MaximumLength(200)
            .WithMessage("Title cannot exceed 200 characters")
            .When(x => x.Request != null);

        RuleFor(x => x.Request.Description)
            .NotEmpty()
            .WithMessage("Description is required")
            .MaximumLength(2000)
            .WithMessage("Description cannot exceed 2000 characters")
            .When(x => x.Request != null);

        RuleFor(x => x.Request.Scenes)
            .NotEmpty()
            .WithMessage("At least one scene is required")
            .When(x => x.Request != null);

        RuleFor(x => x.Request.AgeGroup)
            .NotEmpty()
            .WithMessage("Age group is required")
            .When(x => x.Request != null);
    }
}
