using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.API.Controllers;

/// <summary>
/// Legal pages: Terms of Service and Privacy Policy.
/// Required for GDPR/CCPA compliance before launch.
/// </summary>
[ApiController]
[Route("api/v1/legal")]
public class LegalController : ControllerBase
{
    private readonly ITenantProvider _tenantProvider;
    private readonly AppDbContext _context;

    public LegalController(ITenantProvider tenantProvider, AppDbContext context)
    {
        _tenantProvider = tenantProvider;
        _context = context;
    }
    /// <summary>
    /// Get Terms of Service
    /// </summary>
    [HttpGet("terms")]
    [AllowAnonymous]
    public IActionResult GetTermsOfService()
    {
        return Ok(new
        {
            title = "Terms of Service",
            version = "2.0",
            effectiveDate = "2026-06-23",
            lastUpdated = "2026-06-23",
            company = "Upkilo Technologies Private Limited",
            registeredIn = "India",
            governingLaw = "Laws of India",
            sections = new[]
            {
                new
                {
                    id = "acceptance",
                    title = "1. Acceptance of Terms",
                    content = "By accessing or using the Upkilo platform (\"Service\"), you agree to be bound by these Terms of Service. If you do not agree, you may not use the Service."
                },
                new
                {
                    id = "description",
                    title = "2. Description of Service",
                    content = "Upkilo is a cloud-based SaaS platform that provides appointment scheduling, client management, billing, and business operations tools for service-based businesses."
                },
                new
                {
                    id = "accounts",
                    title = "3. User Accounts",
                    content = "You are responsible for maintaining the confidentiality of your account credentials and for all activities that occur under your account. You must notify us immediately of any unauthorized use."
                },
                new
                {
                    id = "subscription",
                    title = "4. Subscription & Billing",
                    content = "Subscriptions are billed in advance on a monthly or annual basis. You may cancel at any time, and your access will continue until the end of the current billing period. Refunds are not provided for partial periods."
                },
                new
                {
                    id = "data",
                    title = "5. Data Ownership & Portability",
                    content = "You retain ownership of all data you upload to the Service. You may export your data at any time via the built-in export tools. Upon account termination, your data will be retained for 30 days before permanent deletion."
                },
                new
                {
                    id = "privacy",
                    title = "6. Privacy",
                    content = "Your use of the Service is subject to our Privacy Policy. We collect, process, and store your data as described therein."
                },
                new
                {
                    id = "prohibited",
                    title = "7. Prohibited Uses",
                    content = "You may not: (a) use the Service for illegal purposes; (b) attempt to gain unauthorized access; (c) transmit malware or spam; (d) resell the Service without authorization; (e) store or transmit content that infringes intellectual property rights."
                },
                new
                {
                    id = "liability",
                    title = "8. Limitation of Liability",
                    content = "To the maximum extent permitted by applicable law, Upkilo Technologies Private Limited shall not be liable for any indirect, incidental, special, consequential, or punitive damages, or any loss of profits or revenues. Our total aggregate liability shall not exceed the amounts paid by you in the 12 months preceding the claim."
                },
                new
                {
                    id = "termination",
                    title = "9. Termination",
                    content = "We may suspend or terminate your access if you violate these Terms. You may terminate your account at any time through account settings. Upon termination, your data is retained for 30 days then permanently deleted."
                },
                new
                {
                    id = "changes",
                    title = "10. Changes to Terms",
                    content = "We may update these Terms from time to time. We will notify you of material changes via email or in-app notification at least 30 days before they take effect."
                },
                new
                {
                    id = "governing_law",
                    title = "11. Governing Law & Dispute Resolution",
                    content = "These Terms are governed by the laws of India. Any disputes arising out of or in connection with these Terms shall be subject to the exclusive jurisdiction of the competent courts in India. For users located in the European Union or other jurisdictions, mandatory consumer protection laws of your jurisdiction may also apply."
                },
                new
                {
                    id = "grievance",
                    title = "12. Grievance Officer",
                    content = "In accordance with the Information Technology Act, 2000 and the Digital Personal Data Protection Act, 2023, a Grievance Officer has been designated. Name: Grievance Officer, Upkilo Technologies Private Limited. Email: grievance@upkilo.com. Any grievance relating to privacy or data processing will be acknowledged within 24 hours and resolved within 30 days of receipt."
                }
            }
        });
    }

    /// <summary>
    /// Get Privacy Policy
    /// </summary>
    [HttpGet("privacy")]
    [AllowAnonymous]
    public IActionResult GetPrivacyPolicy()
    {
        return Ok(new
        {
            title = "Privacy Policy",
            version = "2.0",
            effectiveDate = "2026-06-23",
            lastUpdated = "2026-06-23",
            company = "Upkilo Technologies Private Limited",
            registeredIn = "India",
            primaryApplicableLaw = "Digital Personal Data Protection Act, 2023 (India)",
            grievanceOfficer = "grievance@upkilo.com",
            dataProtectionContact = "privacy@upkilo.com",
            legalContact = "legal@upkilo.com",
            sections = new[]
            {
                new
                {
                    id = "collection",
                    title = "1. Information We Collect",
                    content = "We collect: (a) Account information (name, email, phone); (b) Business data you input (clients, bookings, services); (c) Usage analytics (page views, feature usage); (d) Device information (browser, IP address); (e) Payment information (processed securely via Stripe)."
                },
                new
                {
                    id = "use",
                    title = "2. How We Use Your Information",
                    content = "We use your data to: (a) Provide and improve the Service; (b) Process payments; (c) Send transactional notifications; (d) Provide customer support; (e) Analyze usage patterns for improvement; (f) Comply with legal obligations."
                },
                new
                {
                    id = "sharing",
                    title = "3. Data Sharing",
                    content = "We do not sell your data. We share data only with: (a) Service providers (Stripe, SendGrid, Twilio, Azure) for service delivery; (b) Law enforcement when legally required; (c) Business partners with your explicit consent."
                },
                new
                {
                    id = "retention",
                    title = "4. Data Retention",
                    content = "We retain your personal data only as long as necessary for the stated purpose or as required by applicable law (DPDP Act, 2023; IT Act, 2000; applicable tax and financial regulations). " +
                              "Specific periods: account data — duration of account + 30 days; audit logs — 90–730 days by subscription tier; " +
                              "login history — 180 days; financial records — 7 years as required by Indian tax law (in anonymised form). " +
                              "You may request deletion at any time."
                },
                new
                {
                    id = "security",
                    title = "5. Data Security",
                    content = "We implement industry-standard security measures: encryption at rest (AES-256) and in transit (TLS 1.3), regular security audits, access controls, and multi-tenant data isolation."
                },
                new
                {
                    id = "rights",
                    title = "6. Your Rights as a Data Principal",
                    content = "Under the Digital Personal Data Protection Act, 2023 (DPDP Act): " +
                              "(a) Right to information about personal data being processed; " +
                              "(b) Right to correction and erasure of inaccurate or incomplete data; " +
                              "(c) Right to grievance redressal — contact grievance@upkilo.com; " +
                              "(d) Right to nominate a representative in the event of incapacity or death; " +
                              "(e) Right to withdraw consent at any time (without affecting prior processing). " +
                              "EU/EEA users additionally have GDPR rights (access, portability, restriction, objection). " +
                              "California users additionally have CCPA rights (Right to Know, Delete, Opt-Out of Sale, Non-Discrimination). " +
                              "Contact privacy@upkilo.com to exercise any of these rights. Requests are addressed within 30 days."
                },
                new
                {
                    id = "cookies",
                    title = "7. Cookies & Tracking",
                    content = "We use essential cookies for authentication and session management. Optional analytics cookies help us improve the Service. You can manage cookie preferences through our cookie consent banner."
                },
                new
                {
                    id = "international",
                    title = "8. Cross-Border Data Transfers",
                    content = "We are incorporated in India and your data is primarily processed in India. " +
                              "Where we transfer personal data to countries outside India for service delivery (via Stripe, SendGrid, Twilio, Azure), " +
                              "we do so in compliance with the Digital Personal Data Protection Act, 2023, " +
                              "and only to countries permitted under the Central Government's notifications under that Act. " +
                              "For EU/EEA users, transfers outside the EEA are covered by Standard Contractual Clauses (SCCs) per Commission Decision 2021/914. " +
                              "A copy of our Data Processing Agreement is available on request."
                },
                new
                {
                    id = "children",
                    title = "9. Children's Privacy",
                    content = "The Service is not directed to children under 16. We do not knowingly collect data from children."
                },
                new
                {
                    id = "legal_bases",
                    title = "10. Lawful Basis for Processing",
                    content = "Under the Digital Personal Data Protection Act, 2023 (India), we process personal data on the following bases: " +
                              "(a) Consent (DPDP Act S.6) — for analytics, marketing, and optional features. You may withdraw consent at any time; " +
                              "(b) Legitimate uses (DPDP Act S.7) — for employment-related purposes, compliance with Indian law, legal proceedings, medical emergencies, and State-mandated functions; " +
                              "(c) Contractual necessity — to provide the service you have subscribed to. " +
                              "For EU/EEA users, the corresponding GDPR Art. 6 bases apply: contract (6(1)(b)), legal obligation (6(1)(c)), consent (6(1)(a)), legitimate interests (6(1)(f))."
                },
                new
                {
                    id = "government_requests",
                    title = "11. Government & Law Enforcement Requests",
                    content = "We are committed to protecting user privacy and will not disclose user information to " +
                              "governments, agencies, organizations, or third parties except where required by applicable " +
                              "law and valid legal process. " +
                              "As a company incorporated in India, we are subject to lawful government requests under: " +
                              "the Information Technology Act, 2000 (Sections 69, 69A, 69B); " +
                              "the Digital Personal Data Protection Act, 2023 (Chapter VII exemptions); " +
                              "the Code of Criminal Procedure, 1973 (Section 91 production orders); " +
                              "and orders from competent Indian courts. " +
                              "Regardless of the requesting jurisdiction or applicable law, when we receive any government request, we: " +
                              "(a) Verify the request is legally valid, formally issued, and carries proper statutory authority; " +
                              "(b) Require formal written submission with the specific legal instrument and statutory citation; " +
                              "(c) Review scope and challenge requests that are overbroad, legally deficient, or disproportionate; " +
                              "(d) Disclose only the minimum data categories strictly necessary to comply with the specific legal obligation; " +
                              "(e) Log every request in our transparency register regardless of outcome; " +
                              "(f) Notify affected users prior to or promptly after compliance, where legally permitted. " +
                              "Unauthorized requests, informal requests lacking legal process, and requests without proper jurisdiction are rejected. " +
                              "Annual transparency statistics are published at GET /api/v1/legal/government-requests/transparency-report. " +
                              "Law enforcement may submit formal requests to legal@upkilo.com."
                },
                new
                {
                    id = "supervisory_authority",
                    title = "12. Grievance Redressal & Regulatory Complaints",
                    content = "Under the DPDP Act, 2023, you may first raise a grievance with our Grievance Officer at grievance@upkilo.com. " +
                              "If unresolved, you may approach the Data Protection Board of India once it is constituted. " +
                              "EU/EEA users may also lodge a complaint with their national supervisory authority (see edpb.europa.eu). " +
                              "UK users may contact the Information Commissioner's Office (ico.org.uk). " +
                              "We encourage you to contact us first — we respond within 30 days."
                },
                new
                {
                    id = "contact",
                    title = "13. Contact Us",
                    content = "Company: Upkilo Technologies Private Limited, India. " +
                              "Grievance Officer (IT Act / DPDP Act): grievance@upkilo.com — responses within 30 days. " +
                              "Privacy inquiries: privacy@upkilo.com. " +
                              "Legal / law enforcement requests: legal@upkilo.com."
                }
            }
        });
    }

    /// <summary>
    /// Get cookie consent configuration
    /// </summary>
    [HttpGet("cookie-consent")]
    [AllowAnonymous]
    public IActionResult GetCookieConsent()
    {
        return Ok(new
        {
            version = "1.0",
            categories = new[]
            {
                new
                {
                    id = "essential",
                    name = "Essential Cookies",
                    description = "Required for authentication, security, and core functionality. Cannot be disabled.",
                    required = true,
                    cookies = new[] { "auth_token", "session_id", "csrf_token", "tenant_context" }
                },
                new
                {
                    id = "analytics",
                    name = "Analytics Cookies",
                    description = "Help us understand how you use the platform to improve our service.",
                    required = false,
                    cookies = new[] { "usage_analytics", "feature_tracking" }
                },
                new
                {
                    id = "marketing",
                    name = "Marketing Cookies",
                    description = "Used for targeted communications and campaign measurement.",
                    required = false,
                    cookies = new[] { "campaign_tracking", "referral_source" }
                }
            }
        });
    }

    /// <summary>
    /// Submit cookie consent preferences.
    /// GDPR Art. 7(1): server-side record required to demonstrate consent was given.
    /// </summary>
    [HttpPost("cookie-consent")]
    [AllowAnonymous]
    public async Task<IActionResult> SaveCookieConsent([FromBody] CookieConsentRequest request)
    {
        Guid? userId = null;
        var sub = (User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);
        if (sub != null && Guid.TryParse(sub, out var parsed)) userId = parsed;

        var record = new CookieConsentRecord
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Essential = true,
            Analytics = request.Analytics,
            Marketing = request.Marketing,
            IpAddress = request.IpAddress ?? HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = HttpContext.Request.Headers["User-Agent"].ToString(),
            ConsentVersion = "1.0",
            ConsentedAt = DateTime.UtcNow
        };

        _context.CookieConsentRecords.Add(record);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Cookie preferences saved",
            consentId = record.Id,
            timestamp = record.ConsentedAt
        });
    }

    /// <summary>
    /// Submit GDPR data deletion request — delegates to PrivacyController.RequestAccountDeletion,
    /// which performs the full soft-delete + Hangfire permanent deletion scheduling.
    /// Kept here as a convenience alias so both /legal and /privacy paths work.
    /// </summary>
    [HttpPost("data-deletion-request")]
    [Authorize]
    public IActionResult RequestDataDeletion([FromBody] DataDeletionRequest request)
    {
        // Redirect callers to the authoritative endpoint that actually schedules deletion.
        // PrivacyController.RequestAccountDeletion performs soft-delete + Hangfire 30-day erasure.
        return RedirectToAction(
            "RequestAccountDeletion",
            "Privacy",
            new { Area = "" });
    }

    /// <summary>
    /// Generate a Data Processing Agreement (DPA) based on tenant info
    /// </summary>
    [HttpGet("dpa-generator")]
    [Authorize]
    public async Task<IActionResult> GenerateDPA()
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var tenant = await _context.Tenants.FindAsync(tenantId.Value);
        var tenantName = tenant?.Name ?? "Your Company";
        
        var dpaContent = $@"
DATA PROCESSING AGREEMENT (DPA)
DPDP Act, 2023 (India) & GDPR Art. 28(3) Compliant

This Data Processing Agreement (""DPA"") is entered into between:
1. Upkilo Technologies Private Limited, incorporated in India (""Data Processor"")
2. {tenantName} (""Data Controller"" / ""Data Fiduciary"" under DPDP Act)

Effective Date: {DateTime.UtcNow:yyyy-MM-dd}
Version: 2.0

1. PURPOSE
The Processor provides a SaaS platform for booking and client management. This involves processing
Personal Data on behalf of the Controller under GDPR Art. 28.

2. SUBJECT MATTER AND DURATION
The Processor processes data related to the Controller's clients, staff, and business operations
to provide the Services. This DPA remains in effect for the duration of the Services agreement
and terminates when all Personal Data has been returned or deleted per Section 9.

3. NATURE AND PURPOSE OF PROCESSING
Processing activities include: storage, retrieval, display, analysis, transmission, and deletion
of appointment, client, and payment data solely for the purpose of providing the Services.

4. CATEGORIES OF DATA SUBJECTS AND PERSONAL DATA
Data subjects: Controller's end-clients, staff members, and business contacts.
Data categories: name, email, phone, appointment history, payment identifiers, IP address.

5. OBLIGATIONS OF THE PROCESSOR (GDPR Art. 28(3))
The Processor shall:
(a) Process Personal Data only on documented instructions from the Controller, including with
    regard to transfers to third countries (Art. 28(3)(a));
(b) Ensure that persons authorised to process have committed to confidentiality (Art. 28(3)(b));
(c) Implement the technical and organisational measures under Art. 32 (see Section 7) (Art. 28(3)(c));
(d) Respect conditions for engaging sub-processors (Art. 28(3)(d)) — see Section 8;
(e) Assist the Controller in fulfilling data subject rights (access, deletion, portability, restriction)
    given the nature of the processing (Art. 28(3)(e));
(f) Assist the Controller in ensuring compliance with Arts. 32-36, including breach notification,
    DPIAs, and prior consultation with supervisory authorities (Art. 28(3)(f));
(g) At the Controller's choice, delete or return all Personal Data upon termination and delete
    existing copies within 30 days, unless Union or Member State law requires storage (Art. 28(3)(g));
(h) Make available all information necessary to demonstrate compliance and allow and contribute
    to audits conducted by the Controller or an authorised auditor (Art. 28(3)(h)).

6. DATA BREACH NOTIFICATION
The Processor will notify the Controller without undue delay, and no later than 72 hours after
becoming aware of a Personal Data breach affecting the Controller's data. Notification will include:
the nature of the breach, categories and approximate number of data subjects and records affected,
likely consequences, and measures taken or proposed.

7. TECHNICAL AND ORGANISATIONAL MEASURES (TOMs)
- Encryption at rest: AES-256
- Encryption in transit: TLS 1.3
- Multi-factor authentication for all administrative access
- Role-based access control; principle of least privilege enforced
- Regular penetration testing and security audits
- Vulnerability management program (Dependabot + quarterly reviews)
- Background checks for employees with access to Personal Data
- Incident response plan with 72-hour breach notification SLA

8. SUB-PROCESSORS
The Controller grants general written authorisation for the following sub-processors.
The Processor will inform the Controller of any intended changes (addition or replacement)
at least 30 days before they take effect, giving the Controller the opportunity to object:
- Microsoft Azure (Cloud infrastructure — EU data centres where configured)
- Stripe Inc. (Payment processing — US, SCCs + EU addendum apply)
- Twilio Inc. (SMS delivery — US, SCCs apply)
- SendGrid / Twilio SendGrid (Email delivery — US, SCCs apply)
- Hangfire (Background job processing — same hosting infrastructure as above)

9. DATA RETURN AND DELETION
Upon termination of the Services or at the Controller's written request:
- Personal Data will be returned in machine-readable format within 30 days, then deleted from
  all Processor systems including backups.
- Financial records required by applicable law (e.g., 7-year tax obligation) will be retained
  in anonymised or minimised form only.
- A written confirmation of deletion will be provided to the Controller.

10. CROSS-BORDER DATA TRANSFERS
The Processor is incorporated in India and primarily processes data in India under the
Digital Personal Data Protection Act, 2023. Where Personal Data is transferred outside India
(via sub-processors listed in Section 8), the Processor ensures compliance with DPDP Act
transfer restrictions and, for EU/EEA Controller data, maintains Standard Contractual Clauses
(SCCs) per EU Commission Decision 2021/914 with each relevant sub-processor.

11. GOVERNMENT AND LAW ENFORCEMENT REQUESTS
The Processor is subject to lawful government requests under Indian law (IT Act 2000 Sections 69,
69A, 69B; DPDP Act 2023 Chapter VII; CrPC Section 91) and orders from competent Indian courts.
The Processor will: (a) promptly notify the Controller of any government request for the
Controller's Personal Data, unless legally prohibited; (b) verify the legal validity of each
request; (c) disclose only the minimum data required by law; (d) challenge overbroad or legally
deficient requests; (e) log all requests in its transparency register. The Processor's annual
transparency report is publicly available at GET /api/v1/legal/government-requests/transparency-report.

12. AUDIT RIGHTS
The Controller has the right to conduct, or commission an independent auditor to conduct,
an audit of the Processor's processing activities and TOMs once per calendar year, with 30 days'
advance written notice. Audit costs are borne by the Controller unless a breach has occurred.

13. GOVERNING LAW AND DISPUTE RESOLUTION
This DPA is governed by the laws of India. Any dispute arising under this DPA shall be referred
to arbitration under the Arbitration and Conciliation Act, 1996 (India). For EU/EEA Controllers,
GDPR Art. 28 requirements are independently satisfied as documented in Section 5 above;
the competent supervisory authority for GDPR compliance is the authority in the Controller's
EU member state.
";

        return Ok(new
        {
            success = true,
            title = "Data Processing Agreement",
            content = dpaContent,
            generatedAt = DateTime.UtcNow,
            controller = tenantName
        });
    }
}

public record CookieConsentRequest(
    bool Essential,
    bool Analytics,
    bool Marketing,
    string? IpAddress = null
);

public record DataDeletionRequest(
    string Reason,
    bool ConfirmDeletion
);
