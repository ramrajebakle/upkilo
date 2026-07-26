using FluentValidation;
using Upkilo.API.Controllers;

namespace Upkilo.API.Validators;

// PaymentsController Validators
public class RefundRequestDtoValidator : AbstractValidator<RefundRequestDto>
{
    public RefundRequestDtoValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(0).When(x => x.Amount.HasValue);
        RuleFor(x => x.Reason).MaximumLength(500);
    }
}

public class CheckoutRequestDtoValidator : AbstractValidator<CheckoutRequestDto>
{
    public CheckoutRequestDtoValidator()
    {
        RuleFor(x => x.PriceId).NotEmpty();
        RuleFor(x => x.SuccessUrl).NotEmpty().Must(uri => Uri.TryCreate(uri, UriKind.Absolute, out _))
            .WithMessage("Invalid SuccessUrl.");
        RuleFor(x => x.CancelUrl).NotEmpty().Must(uri => Uri.TryCreate(uri, UriKind.Absolute, out _))
            .WithMessage("Invalid CancelUrl.");
    }
}

// BillingController Validators
public class BillingCreateCheckoutRequestValidator : AbstractValidator<BillingCreateCheckoutRequest>
{
    public BillingCreateCheckoutRequestValidator()
    {
        RuleFor(x => x.PlanId).NotEmpty();
    }
}

public class ApplyPromoCodeRequestValidator : AbstractValidator<ApplyPromoCodeRequest>
{
    public ApplyPromoCodeRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(20);
    }
}

public class InvoiceSettingsRequestValidator : AbstractValidator<InvoiceSettingsRequest>
{
    public InvoiceSettingsRequestValidator()
    {
        RuleFor(x => x.Prefix).NotEmpty().MaximumLength(10);
        RuleFor(x => x.NextNumber).GreaterThanOrEqualTo(1);
    }
}
