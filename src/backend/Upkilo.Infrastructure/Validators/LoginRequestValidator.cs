using FluentValidation;
using Upkilo.Core.DTOs;

namespace Upkilo.Infrastructure.Validators;

/// <summary>
/// Server-side validation for login requests. Runs before AuthService.LoginAsync so that
/// malformed / oversized inputs are rejected without touching the database.
///
/// Rules:
/// - Email: required, valid RFC-5321 format, max 256 chars
/// - Password: required, max 128 chars (we don't enforce min-length on login — just verify the hash)
/// - Both fields: stripped of leading/trailing whitespace by the caller before validation
///
/// Error messages are intentionally generic ("Invalid request") so that validation failures
/// do not reveal which specific field was rejected to an attacker.
/// </summary>
public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    private const string GenericMessage = "Invalid request.";

    public LoginRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage(GenericMessage)
            .MaximumLength(256).WithMessage(GenericMessage)
            .EmailAddress().WithMessage(GenericMessage)
            // Block obvious HTML/script injection in email field
            .Must(e => !ContainsHtml(e)).WithMessage(GenericMessage);

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage(GenericMessage)
            .MaximumLength(128).WithMessage(GenericMessage);
    }

    private static bool ContainsHtml(string? value)
    {
        if (string.IsNullOrEmpty(value)) return false;
        return value.Contains('<') || value.Contains('>') || value.Contains("javascript:", StringComparison.OrdinalIgnoreCase);
    }
}
