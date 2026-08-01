using FluentValidation;
using Upkilo.API.Controllers;

namespace Upkilo.API.Validators;

public class CreateBookingRequestValidator : AbstractValidator<CreateBookingRequest>
{
    public CreateBookingRequestValidator()
    {
        RuleFor(x => x.ServiceId).NotEmpty();
        RuleFor(x => x.StaffId).NotEmpty();
        RuleFor(x => x.StartTime).NotEmpty().GreaterThan(DateTime.UtcNow)
            .WithMessage("Booking must be in the future.");
        RuleFor(x => x.EndTime).NotEmpty().GreaterThan(x => x.StartTime)
            .WithMessage("End time must be after start time.");
        RuleFor(x => x.GroupSize).GreaterThan(0).LessThanOrEqualTo(100);

        RuleFor(x => x.ClientEmail).EmailAddress().When(x => !string.IsNullOrEmpty(x.ClientEmail));
        RuleFor(x => x.ClientId).NotEmpty().When(x => string.IsNullOrEmpty(x.ClientEmail))
            .WithMessage("Either ClientId or ClientEmail must be provided.");
    }
}

public class UpdateBookingRequestValidator : AbstractValidator<UpdateBookingRequest>
{
    public UpdateBookingRequestValidator()
    {
        RuleFor(x => x.EndTime).GreaterThan(x => x.StartTime.Value).When(x => x.StartTime.HasValue && x.EndTime.HasValue)
            .WithMessage("End time must be after start time.");
    }
}
