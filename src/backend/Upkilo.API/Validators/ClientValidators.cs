using FluentValidation;
using Upkilo.API.Controllers;

namespace Upkilo.API.Validators;

public class CreateClientRequestValidator : AbstractValidator<CreateClientRequest>
{
    public CreateClientRequestValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(50);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrEmpty(x.Email));
        RuleFor(x => x.Phone).Matches(@"^\+?[1-9]\d{1,14}$")
            .WithMessage("Invalid phone number format.")
            .When(x => !string.IsNullOrEmpty(x.Phone));
        
        RuleFor(x => x).Must(x => !string.IsNullOrEmpty(x.Email) || !string.IsNullOrEmpty(x.Phone))
            .WithMessage("Either Email or Phone must be provided.");
    }
}

public class UpdateClientRequestValidator : AbstractValidator<UpdateClientRequest>
{
    public UpdateClientRequestValidator()
    {
        RuleFor(x => x.FirstName).MaximumLength(50).When(x => x.FirstName != null);
        RuleFor(x => x.LastName).MaximumLength(50).When(x => x.LastName != null);
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrEmpty(x.Email));
    }
}

public class AdjustPointsRequestValidator : AbstractValidator<AdjustPointsRequest>
{
    public AdjustPointsRequestValidator()
    {
        RuleFor(x => x.Points).NotEqual(0);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(200);
    }
}
