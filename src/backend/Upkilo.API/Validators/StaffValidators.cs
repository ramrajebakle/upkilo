using FluentValidation;
using Upkilo.API.Controllers;

namespace Upkilo.API.Validators;

public class CreateStaffRequestValidator : AbstractValidator<CreateStaffRequest>
{
    public CreateStaffRequestValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(50);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Role).NotEmpty();
        RuleFor(x => x.Color).Matches("^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$").When(x => !string.IsNullOrEmpty(x.Color))
            .WithMessage("Color must be a valid hex code.");
    }
}

public class UpdateStaffRequestValidator : AbstractValidator<UpdateStaffRequest>
{
    public UpdateStaffRequestValidator()
    {
        RuleFor(x => x.FirstName).MaximumLength(50).When(x => x.FirstName != null);
        RuleFor(x => x.LastName).MaximumLength(50).When(x => x.LastName != null);
        RuleFor(x => x.Email).EmailAddress().When(x => x.Email != null);
        RuleFor(x => x.Color).Matches("^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$").When(x => !string.IsNullOrEmpty(x.Color))
            .WithMessage("Color must be a valid hex code.");
    }
}
