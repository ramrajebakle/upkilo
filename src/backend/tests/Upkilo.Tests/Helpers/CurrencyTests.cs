using FluentAssertions;
using Upkilo.Core.Helpers;
using Xunit;

namespace Upkilo.Tests.Helpers;

/// <summary>
/// Money-conversion tests.
///
/// These cover the class of bug where a decimal amount is scaled to the minor units a payment
/// processor expects. The codebase previously applied a flat *100 / /100 regardless of currency,
/// which is correct only for the 2-decimal majority. For a zero-decimal currency it inflated the
/// charge by 100x.
/// </summary>
public class CurrencyTests
{
    // ── Zero-decimal currencies ──────────────────────────────────────────

    [Theory]
    [InlineData("JPY")]
    [InlineData("KRW")]
    [InlineData("VND")]
    [InlineData("XOF")]
    public void ToMinorUnits_ZeroDecimalCurrency_DoesNotScale(string code)
    {
        // ¥5,000 must be submitted as 5000, not 500000.
        Currency.ToMinorUnits(5000m, code).Should().Be(5000);
    }

    [Fact]
    public void ToMinorUnits_Jpy_IsNotHundredTimesUsd()
    {
        // The regression this suite exists for: same face value, different scaling.
        Currency.ToMinorUnits(5000m, "JPY").Should().Be(5000);
        Currency.ToMinorUnits(5000m, "USD").Should().Be(500000);
    }

    [Fact]
    public void FromMinorUnits_ZeroDecimalCurrency_DoesNotDivide()
    {
        // Reading a ¥5,000 invoice back must not yield ¥50.
        Currency.FromMinorUnits(5000, "JPY").Should().Be(5000m);
    }

    // ── Two-decimal currencies ───────────────────────────────────────────

    [Theory]
    [InlineData("USD", 39.00, 3900)]
    [InlineData("EUR", 89.50, 8950)]
    [InlineData("INR", 2999.00, 299900)]
    [InlineData("GBP", 0.01, 1)]
    public void ToMinorUnits_TwoDecimalCurrency_ScalesByHundred(string code, double amount, long expected)
    {
        Currency.ToMinorUnits((decimal)amount, code).Should().Be(expected);
    }

    [Fact]
    public void ToMinorUnits_RoundsHalfAwayFromZero()
    {
        // Banker's rounding would give 2 here and quietly lose a cent on half the cases.
        Currency.ToMinorUnits(0.025m, "USD").Should().Be(3);
    }

    // ── Three-decimal currencies ─────────────────────────────────────────

    [Fact]
    public void ToMinorUnits_ThreeDecimalCurrency_UsesThousandths()
    {
        // KWD 1.500 -> 1500 minor units.
        Currency.ToMinorUnits(1.500m, "KWD").Should().Be(1500);
    }

    [Theory]
    [InlineData("KWD")]
    [InlineData("BHD")]
    [InlineData("OMR")]
    public void ToMinorUnits_ThreeDecimalCurrency_IsDivisibleByTen(string code)
    {
        // Stripe rejects three-decimal amounts that are not multiples of 10.
        var result = Currency.ToMinorUnits(1.234m, code);
        (result % 10).Should().Be(0);
    }

    // ── Round-tripping ───────────────────────────────────────────────────

    [Theory]
    [InlineData("USD", 39.00)]
    [InlineData("JPY", 5000)]
    [InlineData("INR", 2999.00)]
    [InlineData("KRW", 45000)]
    public void MinorUnits_RoundTrips(string code, double amount)
    {
        var value = (decimal)amount;
        Currency.FromMinorUnits(Currency.ToMinorUnits(value, code), code).Should().Be(value);
    }

    // ── Robustness: the payment path must not throw ──────────────────────

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("ZZZ")]
    [InlineData("not-a-currency")]
    public void ToMinorUnits_UnknownOrMissingCode_FallsBackToTwoDecimals(string? code)
    {
        // Degrading to the common case keeps a bad code from becoming an outage.
        Currency.ToMinorUnits(10m, code).Should().Be(1000);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("ZZZ")]
    public void Format_UnknownOrMissingCode_DoesNotThrow(string? code)
    {
        Currency.Format(1234.5m, code).Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Format_ZeroDecimalCurrency_OmitsDecimalPlaces()
    {
        Currency.Format(5000m, "JPY").Should().Be("¥5,000");
    }

    [Fact]
    public void Format_TwoDecimalCurrency_KeepsDecimalPlaces()
    {
        Currency.Format(39m, "USD").Should().Be("$39.00");
    }

    // ── Validation surface ───────────────────────────────────────────────

    [Theory]
    [InlineData("USD")]
    [InlineData("usd")]
    [InlineData(" JPY ")]
    [InlineData("INR")]
    public void IsSupported_CataloguedCode_IsTrueRegardlessOfCasingOrPadding(string code)
    {
        Currency.IsSupported(code).Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("ZZZ")]
    [InlineData("'; DROP TABLE Tenants; --")]
    public void IsSupported_UnknownCode_IsFalse(string? code)
    {
        Currency.IsSupported(code).Should().BeFalse();
    }

    [Fact]
    public void Catalogue_CoversEveryRegionalPaymentCurrency()
    {
        // GlobalPaymentsController accepts local payment methods in these currencies, so a
        // tenant in those markets must be able to select and be billed in them.
        var regional = new[] { "INR", "THB", "SGD", "IDR", "SAR", "AED", "EGP", "BRL", "MXN", "JPY" };
        foreach (var code in regional)
            Currency.IsSupported(code).Should().BeTrue($"{code} is offered by regional payments");
    }

    [Fact]
    public void Normalize_TrimsAndUppercases()
    {
        Currency.Normalize(" jpy ").Should().Be("JPY");
        Currency.Normalize(null).Should().Be("USD");
    }
}
