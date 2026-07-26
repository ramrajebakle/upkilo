using FluentValidation;
using Upkilo.API.Controllers;
using Upkilo.Core.DTOs;

namespace Upkilo.API.Validators;

public class CreatePlanRequestValidator : AbstractValidator<CreatePlanRequest>
{
    public CreatePlanRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.MonthlyPrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.YearlyPrice).GreaterThanOrEqualTo(0);
    }
}

public class UpdateFeatureFlagRequestValidator : AbstractValidator<UpdateFeatureFlagRequest>
{
    public UpdateFeatureFlagRequestValidator()
    {
        RuleFor(x => x.RolloutPercent).InclusiveBetween(0, 100).When(x => x.RolloutPercent.HasValue);
    }
}

public class SendAnnouncementRequestValidator : AbstractValidator<SendAnnouncementRequest>
{
    public SendAnnouncementRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Message).NotEmpty();
        RuleFor(x => x.Type).Must(x => new[] { "info", "warning", "critical" }.Contains(x.ToLower()))
            .WithMessage("Invalid announcement type. Must be info, warning, or critical.");
    }
}
