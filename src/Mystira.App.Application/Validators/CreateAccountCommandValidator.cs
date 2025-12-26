using FluentValidation;
using Mystira.App.Application.CQRS.Accounts.Commands;

namespace Mystira.App.Application.Validators;

/// <summary>
/// Validates CreateAccountCommand before processing.
/// </summary>
public class CreateAccountCommandValidator : AbstractValidator<CreateAccountCommand>
{
    public CreateAccountCommandValidator()
    {
        RuleFor(x => x.Auth0UserId)
            .NotEmpty()
            .WithMessage("Auth0 user ID is required")
            .MaximumLength(128)
            .WithMessage("Auth0 user ID cannot exceed 128 characters");

        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email is required")
            .EmailAddress()
            .WithMessage("A valid email address is required")
            .MaximumLength(256)
            .WithMessage("Email cannot exceed 256 characters");

        RuleFor(x => x.DisplayName)
            .MaximumLength(100)
            .WithMessage("Display name cannot exceed 100 characters")
            .When(x => !string.IsNullOrEmpty(x.DisplayName));
    }
}
