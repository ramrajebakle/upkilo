using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;
using System.Security.Claims;
using System.Security.Cryptography.Xml;
using System.Security.Cryptography.X509Certificates;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Upkilo.Core.Entities;

namespace Upkilo.Infrastructure.Services;

public class SamlUserResult
{
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string AssertionId { get; set; } = string.Empty; // F-07: one-time-use replay key
    public DateTime? NotOnOrAfter { get; set; }              // F-07: replay-cache TTL bound
}

/// <summary>
/// Implements SAML 2.0 and Enterprise SSO flow validation and generation.
/// </summary>
public class SsoIntegrationService
{
    private readonly ILogger<SsoIntegrationService> _logger;

    public SsoIntegrationService(ILogger<SsoIntegrationService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Generates a SAML AuthnRequest XML, compresses it using DEFLATE, and returns it as a Base64-encoded string.
    /// </summary>
    public string CreateSamlRequest(string issuer, string acsUrl, string destination)
    {
        var id = $"_auth_{Guid.NewGuid():N}";
        var issueInstant = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
        
        var xml = $@"<samlp:AuthnRequest xmlns:samlp=""urn:oasis:names:tc:SAML:2.0:protocol"" xmlns:saml=""urn:oasis:names:tc:SAML:2.0:assertion"" ID=""{id}"" Version=""2.0"" IssueInstant=""{issueInstant}"" Destination=""{destination}"" AssertionConsumerServiceURL=""{acsUrl}""><saml:Issuer>{issuer}</saml:Issuer><samlp:NameIDPolicy Format=""urn:oasis:names:tc:SAML:1.1:nameid-format:emailAddress"" AllowCreate=""true"" /></samlp:AuthnRequest>";
        
        var bytes = Encoding.UTF8.GetBytes(xml);
        using var output = new MemoryStream();
        using (var zip = new DeflateStream(output, CompressionMode.Compress))
        {
            zip.Write(bytes, 0, bytes.Length);
        }
        return Convert.ToBase64String(output.ToArray());
    }

    /// <summary>
    /// Decodes a base64-encoded SAMLResponse, verifies its XML signature, validates timestamps and audience restrictions, and maps user attributes.
    /// </summary>
    public SamlUserResult ValidateSamlResponse(string samlResponseBase64, SamlConfiguration samlConfig, string expectedAudience)
    {
        _logger.LogInformation("Validating SAML response for tenant {TenantId}", samlConfig.TenantId);

        if (string.IsNullOrEmpty(samlResponseBase64))
        {
            throw new ArgumentException("SAMLResponse cannot be null or empty.");
        }

        // Base64-decode the SAMLResponse
        var rawResponseBytes = Convert.FromBase64String(samlResponseBase64);
        var xmlString = Encoding.UTF8.GetString(rawResponseBytes);

        // Load XML safely (XXE protection)
        var xmlDoc = new XmlDocument();
        xmlDoc.XmlResolver = null;
        using (var stringReader = new StringReader(xmlString))
        {
            var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit };
            using (var xmlReader = XmlReader.Create(stringReader, settings))
            {
                xmlDoc.Load(xmlReader);
            }
        }

        var nsmgr = new XmlNamespaceManager(xmlDoc.NameTable);
        nsmgr.AddNamespace("samlp", "urn:oasis:names:tc:SAML:2.0:protocol");
        nsmgr.AddNamespace("saml", "urn:oasis:names:tc:SAML:2.0:assertion");
        nsmgr.AddNamespace("ds", "http://www.w3.org/2000/09/xmldsig#");

        // Verify XML Signature
        if (string.IsNullOrEmpty(samlConfig.IdpCertificate))
        {
            throw new InvalidOperationException("IdP Certificate is not configured for this tenant.");
        }

        // F-07: Require EXACTLY ONE signature. Multiple <ds:Signature> nodes are a hallmark of
        // XML Signature Wrapping (XSW) — reject outright.
        var signatureNodes = xmlDoc.SelectNodes("//ds:Signature", nsmgr);
        if (signatureNodes == null || signatureNodes.Count == 0)
        {
            throw new Exception("SAML Response signature not found.");
        }
        if (signatureNodes.Count > 1)
        {
            throw new Exception("Multiple signatures detected — possible signature-wrapping attack.");
        }
        var signatureNode = (XmlElement)signatureNodes[0]!;

        var signedXml = new SignedXml(xmlDoc);
        signedXml.LoadXml(signatureNode);

        var certBytes = Convert.FromBase64String(CleanCertificate(samlConfig.IdpCertificate));
        using var cert = new X509Certificate2(certBytes);

        bool isSignatureValid = signedXml.CheckSignature(cert, true);
        if (!isSignatureValid)
        {
            throw new Exception("SAML XML signature validation failed.");
        }

        // F-07 (XSW core): bind ALL subsequent reads to the element the signature actually
        // covers. Reading issuer/audience/NameID via document-wide "//" XPath is exactly what
        // lets an attacker inject a forged assertion alongside a validly-signed one.
        var signedElement = ResolveSignedElement(xmlDoc, signedXml);

        XmlElement assertion;
        if (signedElement.LocalName == "Assertion")
        {
            assertion = signedElement;
        }
        else
        {
            var assertionNodes = signedElement.SelectNodes(".//saml:Assertion", nsmgr);
            if (assertionNodes == null || assertionNodes.Count == 0)
                throw new Exception("No SAML Assertion found within the signed element.");
            if (assertionNodes.Count > 1)
                throw new Exception("Multiple assertions in signed scope — possible wrapping attack.");
            assertion = (XmlElement)assertionNodes[0]!;
        }

        // Validate Issuer (scoped to the signed assertion)
        var issuerNode = assertion.SelectSingleNode("saml:Issuer", nsmgr)
                         ?? assertion.SelectSingleNode(".//saml:Issuer", nsmgr);
        if (issuerNode == null)
        {
            throw new Exception("SAML Issuer not found in signed assertion.");
        }
        if (!string.Equals(issuerNode.InnerText.Trim(), samlConfig.EntityId?.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            throw new Exception($"SAML Issuer mismatch. Expected: {samlConfig.EntityId}, Got: {issuerNode.InnerText}");
        }

        // Validate Audience (scoped)
        var audienceNode = assertion.SelectSingleNode(".//saml:Audience", nsmgr);
        if (audienceNode == null)
        {
            throw new Exception("SAML Audience not found in signed assertion.");
        }
        if (!string.Equals(audienceNode.InnerText.Trim(), expectedAudience?.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            throw new Exception($"SAML Audience mismatch. Expected: {expectedAudience}, Got: {audienceNode.InnerText}");
        }

        // Validate Conditions Time Constraints (scoped) and capture NotOnOrAfter for replay TTL
        DateTime? assertionNotOnOrAfter = null;
        var conditionsNode = assertion.SelectSingleNode("saml:Conditions", nsmgr) as XmlElement;
        if (conditionsNode != null)
        {
            var notBeforeStr = conditionsNode.GetAttribute("NotBefore");
            var notOnOrAfterStr = conditionsNode.GetAttribute("NotOnOrAfter");

            var now = DateTime.UtcNow;
            var skew = TimeSpan.FromMinutes(5); // 5 minutes clock skew tolerance

            if (!string.IsNullOrEmpty(notBeforeStr) && DateTime.TryParse(notBeforeStr, out var notBefore))
            {
                if (now < notBefore.ToUniversalTime() - skew)
                    throw new Exception("SAML Assertion is not yet valid (NotBefore constraint failed).");
            }

            if (!string.IsNullOrEmpty(notOnOrAfterStr) && DateTime.TryParse(notOnOrAfterStr, out var notOnOrAfter))
            {
                assertionNotOnOrAfter = notOnOrAfter.ToUniversalTime();
                if (now >= assertionNotOnOrAfter.Value + skew)
                    throw new Exception("SAML Assertion has expired (NotOnOrAfter constraint failed).");
            }
        }

        // Capture the assertion ID for one-time-use replay protection (enforced by the caller).
        var assertionId = assertion.GetAttribute("ID");

        // Extract NameID (Email) — scoped
        var nameIdNode = assertion.SelectSingleNode(".//saml:NameID", nsmgr);
        if (nameIdNode == null || string.IsNullOrEmpty(nameIdNode.InnerText))
        {
            throw new Exception("SAML NameID is missing or empty.");
        }
        var email = nameIdNode.InnerText.Trim();

        // Extract attributes for dynamic mapping — scoped to the signed assertion
        var attributeNodes = assertion.SelectNodes(".//saml:Attribute", nsmgr);
        var attributes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (attributeNodes != null)
        {
            foreach (XmlNode node in attributeNodes)
            {
                var nameAttr = node.Attributes?["Name"]?.Value;
                var valNode = node.SelectSingleNode("saml:AttributeValue", nsmgr);
                if (!string.IsNullOrEmpty(nameAttr) && valNode != null)
                {
                    attributes[nameAttr] = valNode.InnerText.Trim();
                }
            }
        }

        // Parse custom mappings if configured
        string firstName = string.Empty;
        string lastName = string.Empty;

        if (!string.IsNullOrEmpty(samlConfig.AttributeMapping))
        {
            try
            {
                var mappings = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(samlConfig.AttributeMapping);
                if (mappings != null)
                {
                    if (mappings.TryGetValue("firstName", out var fnClaim) && !string.IsNullOrEmpty(fnClaim) && attributes.TryGetValue(fnClaim, out var fnVal))
                    {
                        firstName = fnVal;
                    }
                    if (mappings.TryGetValue("lastName", out var lnClaim) && !string.IsNullOrEmpty(lnClaim) && attributes.TryGetValue(lnClaim, out var lnVal))
                    {
                        lastName = lnVal;
                    }
                    if (mappings.TryGetValue("email", out var emailClaim) && !string.IsNullOrEmpty(emailClaim) && attributes.TryGetValue(emailClaim, out var emailVal))
                    {
                        email = emailVal;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse SAML AttributeMapping JSON for tenant {TenantId}", samlConfig.TenantId);
            }
        }

        // Sensible fallbacks if attributes weren't matched by JSON
        if (string.IsNullOrEmpty(firstName))
        {
            firstName = FindAttributeValue(attributes, "firstName", "first_name", "givenName", "given_name", "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/givenname");
        }
        if (string.IsNullOrEmpty(lastName))
        {
            lastName = FindAttributeValue(attributes, "lastName", "last_name", "surName", "surname", "sn", "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/surname");
        }

        // If still empty, fall back to email prefix or generic names
        if (string.IsNullOrEmpty(firstName))
        {
            firstName = email.Split('@')[0];
        }

        return new SamlUserResult
        {
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            AssertionId = assertionId,
            NotOnOrAfter = assertionNotOnOrAfter
        };
    }

    /// <summary>
    /// F-07: Resolves the element actually covered by the signature, via its Reference URI.
    /// All trusted reads must come from within this element to defeat signature wrapping.
    /// </summary>
    private static XmlElement ResolveSignedElement(XmlDocument doc, SignedXml signedXml)
    {
        var references = signedXml.SignedInfo?.References;
        var reference = references != null && references.Count > 0
            ? references[0] as Reference
            : null;
        var uri = reference?.Uri;

        // Empty URI = enveloped signature over the entire document.
        if (string.IsNullOrEmpty(uri))
            return doc.DocumentElement ?? throw new Exception("SAML document has no root element.");

        if (!uri.StartsWith("#", StringComparison.Ordinal))
            throw new Exception("Unsupported SAML signature reference (external URIs are not allowed).");

        var id = uri.Substring(1);
        return FindElementById(doc.DocumentElement, id)
            ?? throw new Exception("Signed SAML element not found for the signature reference.");
    }

    /// <summary>
    /// Manual ID lookup (XmlDocument.GetElementById needs a DTD/schema we deliberately do not
    /// load). Matches the SAML ID attribute conventions.
    /// </summary>
    private static XmlElement? FindElementById(XmlNode? node, string id)
    {
        if (node is XmlElement el)
        {
            foreach (var attrName in new[] { "ID", "AssertionID", "ResponseID" })
            {
                var val = el.GetAttribute(attrName);
                if (!string.IsNullOrEmpty(val) && string.Equals(val, id, StringComparison.Ordinal))
                    return el;
            }
        }
        if (node != null)
        {
            foreach (XmlNode child in node.ChildNodes)
            {
                var found = FindElementById(child, id);
                if (found != null) return found;
            }
        }
        return null;
    }

    private string FindAttributeValue(Dictionary<string, string> attributes, params string[] possibleKeys)
    {
        foreach (var key in possibleKeys)
        {
            if (attributes.TryGetValue(key, out var val))
            {
                return val;
            }
        }
        return string.Empty;
    }

    private static string CleanCertificate(string certPem)
    {
        if (string.IsNullOrEmpty(certPem)) return string.Empty;
        return certPem
            .Replace("-----BEGIN CERTIFICATE-----", "")
            .Replace("-----END CERTIFICATE-----", "")
            .Replace("\r", "")
            .Replace("\n", "")
            .Replace(" ", "")
            .Trim();
    }

    public async Task<ClaimsPrincipal> AuthenticateExternalSsoAsync(string provider, string tenantSsoId)
    {
        _logger.LogInformation("Authenticating via {Provider} for SSO ID {Id}", provider, tenantSsoId);
        await Task.Delay(10);
        return new ClaimsPrincipal(); 
    }

    public async Task ProvisionTenantSsoAsync(Guid tenantId, string provider, string metadataUrl)
    {
        _logger.LogInformation("Configuring enterprise federation for {TenantId} using {Provider}", tenantId, provider);
        await Task.Delay(10);
    }
}
