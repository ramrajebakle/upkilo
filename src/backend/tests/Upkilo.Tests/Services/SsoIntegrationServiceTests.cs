using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Upkilo.Core.Entities;
using Upkilo.Infrastructure.Services;
using Xunit;

namespace Upkilo.Tests.Services;

public class SsoIntegrationServiceTests
{
    private SsoIntegrationService CreateSut()
    {
        var logger = new Mock<ILogger<SsoIntegrationService>>();
        return new SsoIntegrationService(logger.Object);
    }

    [Fact]
    public void CreateSamlRequest_GeneratesValidBase64DeflatedXml()
    {
        // Arrange
        var sut = CreateSut();
        var issuer = "https://upkilo.com/sso/metadata";
        var acsUrl = "https://upkilo.com/sso/callback";
        var destination = "https://idp.com/saml/login";

        // Act
        var resultBase64 = sut.CreateSamlRequest(issuer, acsUrl, destination);

        // Assert
        resultBase64.Should().NotBeNullOrWhiteSpace();

        // Decode Base64
        var compressedBytes = Convert.FromBase64String(resultBase64);

        // Decompress
        using var input = new MemoryStream(compressedBytes);
        using var unzip = new DeflateStream(input, CompressionMode.Decompress);
        using var reader = new StreamReader(unzip, Encoding.UTF8);
        var xml = reader.ReadToEnd();

        xml.Should().Contain("<samlp:AuthnRequest");
        xml.Should().Contain(issuer);
        xml.Should().Contain(acsUrl);
        xml.Should().Contain(destination);
    }

    [Fact]
    public void ValidateSamlResponse_WhenResponseIsEmpty_ThrowsArgumentException()
    {
        // Arrange
        var sut = CreateSut();
        var config = new SamlConfiguration { TenantId = Guid.NewGuid() };

        // Act
        var act = () => sut.ValidateSamlResponse("", config, "https://upkilo.com/sso/metadata");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ValidateSamlResponse_WhenXmlHasNoSignature_ThrowsException()
    {
        // Arrange
        var sut = CreateSut();
        var config = new SamlConfiguration 
        { 
            TenantId = Guid.NewGuid(),
            IdpCertificate = "dummycert"
        };

        // Simple SAML Response XML with no signature
        var xml = "<samlp:Response xmlns:samlp=\"urn:oasis:names:tc:SAML:2.0:protocol\"></samlp:Response>";
        var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(xml));

        // Act
        var act = () => sut.ValidateSamlResponse(base64, config, "https://upkilo.com/sso/metadata");

        // Assert
        act.Should().Throw<Exception>().WithMessage("SAML Response signature not found.");
    }
}
