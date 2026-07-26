using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Services;
using Upkilo.Tests.Helpers;

namespace Upkilo.Tests.Validators;

/// <summary>
/// Tests for RegisterRequestValidator — validates all FluentValidation rules.
/// </summary>
public class RegisterRequestValidatorTests
{
    private readonly Upkilo.Infrastructure.Validators.RegisterRequestValidator _sut = new();

    [Fact]
    public void Validate_ValidRequest_IsValid()
    {
        var request = new Upkilo.Core.DTOs.RegisterRequest("user@example.com", "StrongP@ss1!", "John", "Doe", "Acme Inc");
        var result = _sut.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    [InlineData("@missing-local.com")]
    public void Validate_InvalidEmail_Fails(string email)
    {
        var request = new Upkilo.Core.DTOs.RegisterRequest(email, "StrongP@ss1!", "John", "Doe", null);
        var result = _sut.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Theory]
    [InlineData("")]        // empty
    [InlineData("short")]   // too short, missing uppercase/number/special
    [InlineData("alllowercase1!")] // missing uppercase
    [InlineData("ALLUPPERCASE1!")] // missing lowercase
    [InlineData("NoNumber!Aa")]    // missing number
    [InlineData("NoSpecial1Aa")]   // missing special char
    public void Validate_WeakPassword_Fails(string password)
    {
        var request = new Upkilo.Core.DTOs.RegisterRequest("user@example.com", password, "John", "Doe", null);
        var result = _sut.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password");
    }

    [Fact]
    public void Validate_EmptyFirstName_Fails()
    {
        var request = new Upkilo.Core.DTOs.RegisterRequest("user@example.com", "StrongP@ss1!", "", "Doe", null);
        var result = _sut.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "FirstName");
    }

    [Fact]
    public void Validate_EmptyLastName_Fails()
    {
        var request = new Upkilo.Core.DTOs.RegisterRequest("user@example.com", "StrongP@ss1!", "John", "", null);
        var result = _sut.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "LastName");
    }

    [Fact]
    public void Validate_FirstNameTooLong_Fails()
    {
        var longName = new string('A', 51);
        var request = new Upkilo.Core.DTOs.RegisterRequest("user@example.com", "StrongP@ss1!", longName, "Doe", null);
        var result = _sut.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "FirstName");
    }

    [Fact]
    public void Validate_LastNameTooLong_Fails()
    {
        var longName = new string('B', 51);
        var request = new Upkilo.Core.DTOs.RegisterRequest("user@example.com", "StrongP@ss1!", "John", longName, null);
        var result = _sut.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "LastName");
    }

    [Fact]
    public void Validate_CompanyNameTooLong_Fails()
    {
        var longCompany = new string('C', 101);
        var request = new Upkilo.Core.DTOs.RegisterRequest("user@example.com", "StrongP@ss1!", "John", "Doe", longCompany);
        var result = _sut.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CompanyName");
    }

    [Fact]
    public void Validate_NullCompanyName_IsValid()
    {
        var request = new Upkilo.Core.DTOs.RegisterRequest("user@example.com", "StrongP@ss1!", "John", "Doe", null);
        var result = _sut.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_PasswordMinLength8_IsValid()
    {
        var request = new Upkilo.Core.DTOs.RegisterRequest("user@example.com", "Abcde1!x", "John", "Doe", null);
        var result = _sut.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_MultipleErrors_ReturnsAll()
    {
        var request = new Upkilo.Core.DTOs.RegisterRequest("", "", "", "", null);
        var result = _sut.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Count.Should().BeGreaterOrEqualTo(4); // Email, Password, FirstName, LastName
    }
}
