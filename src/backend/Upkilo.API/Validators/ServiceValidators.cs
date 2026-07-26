using FluentValidation;
using Upkilo.API.Controllers;

namespace Upkilo.API.Validators;

public class CreateServiceRequestValidator : AbstractValidator<CreateServiceRequest>
{
    public CreateServiceRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.DurationMinutes).GreaterThan(0);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
        RuleFor(x => x.BufferBefore).GreaterThanOrEqualTo(0);
        RuleFor(x => x.BufferAfter).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MaxAttendees).GreaterThan(0);
        RuleFor(x => x.DepositAmount).GreaterThanOrEqualTo(0).When(x => x.RequiresPayment && x.DepositAmount.HasValue);
    }
}

public class UpdateServiceRequestValidator : AbstractValidator<UpdateServiceRequest>
{
    public UpdateServiceRequestValidator()
    {
        RuleFor(x => x.Name).MaximumLength(100).When(x => x.Name != null);
        RuleFor(x => x.DurationMinutes).GreaterThan(0).When(x => x.DurationMinutes.HasValue);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0).When(x => x.Price.HasValue);
    }
}
