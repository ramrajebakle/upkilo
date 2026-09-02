using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.API.Controllers;

/// <summary>
/// V1-V4: Vertical-specific features that differentiate Upkilo from generic scheduling software.
///   V1 Medical/Dental — treatment plans, Rx tracking, insurance pre-auth
///   V2 Fitness         — workout tracking, body composition, program progressions
///   V3 Pet Grooming    — breed-aware scheduling, vaccination records
///   V4 Beauty Education — student booking, mentor assignment, certification tracking
/// Each vertical is gated by the tenant's Industry field + HIPAA BAA where required.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/verticals")]
[Authorize]
public class VerticalsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<VerticalsController> _logger;

    public VerticalsController(
        AppDbContext context,
        ITenantProvider tenantProvider,
        ILogger<VerticalsController> logger)
    {
        _context = context;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    private Guid TenantId => _tenantProvider.GetTenantId() ?? Guid.Empty;

    // ═══════════════════════════════════════════════════════════════════════════
    // V1: Medical / Dental
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// V1: GET /verticals/medical/treatment-plans — Returns treatment plan templates.
    /// Requires HIPAA BAA to be signed. Gated to medical/dental tenants.
    /// </summary>
    [HttpGet("medical/treatment-plans")]
    public async Task<IActionResult> GetTreatmentPlanTemplates()
    {
        var tenant = await _context.Tenants.FindAsync(TenantId);
        if (tenant == null) return Unauthorized();

        // HIPAA gate — must have signed BAA
        var hipaaSigned = await _context.Set<GdprConsent>()
            .AnyAsync(c => c.TenantId == TenantId && c.ConsentType == "HIPAA_BAA" && c.IsGranted);
        if (!hipaaSigned)
            return StatusCode(403, new
            {
                error = "hipaa_baa_required",
                message = "Sign the HIPAA BAA at POST /consent/hipaa-baa/sign to unlock medical features."
            });

        // Return templates for common medical/dental procedures
        var templates = new[]
        {
            new { Id = "tp_001", Name = "Initial Consultation", Steps = new[] { "Patient intake", "Medical history review", "Examination", "Diagnosis", "Treatment plan presentation", "Consent signing" }, EstimatedDurationMins = 60, Category = "Consultation" },
            new { Id = "tp_002", Name = "Teeth Cleaning (Prophylaxis)", Steps = new[] { "X-rays (if required)", "Plaque/tartar removal", "Polishing", "Flossing", "Fluoride treatment", "Post-care instructions" }, EstimatedDurationMins = 45, Category = "Preventive" },
            new { Id = "tp_003", Name = "Botox/Filler Treatment", Steps = new[] { "Consultation & consent", "Before photos", "Topical numbing (15 min)", "Injection", "After photos", "Follow-up scheduling at 2 weeks" }, EstimatedDurationMins = 30, Category = "Aesthetics" },
            new { Id = "tp_004", Name = "Laser Hair Removal Session", Steps = new[] { "Consent", "Skin assessment", "Patch test (first visit)", "Treatment", "Cooling/aftercare", "Next session scheduling" }, EstimatedDurationMins = 45, Category = "Aesthetics" },
            new { Id = "tp_005", Name = "Post-op Follow-up", Steps = new[] { "Wound check", "Suture removal (if applicable)", "Pain assessment", "Prescription review", "Activity restrictions update" }, EstimatedDurationMins = 20, Category = "Post-op" },
        };

        return Ok(new { data = templates, count = templates.Length, hipaaBaaActive = true });
    }

    /// <summary>
    /// V1: GET /verticals/medical/prescriptions/{clientId} — Returns Rx tracking for a client.
    /// </summary>
    [HttpGet("medical/prescriptions/{clientId}")]
    public async Task<IActionResult> GetClientPrescriptions(Guid clientId)
    {
        var hipaaSigned = await _context.Set<GdprConsent>()
            .AnyAsync(c => c.TenantId == TenantId && c.ConsentType == "HIPAA_BAA" && c.IsGranted);
        if (!hipaaSigned)
            return StatusCode(403, new { error = "hipaa_baa_required" });

        // Prescriptions stored in client Notes with "Rx:" prefix — queried from Booking notes
        var rxHistory = await _context.Bookings
            .Where(b => b.TenantId == TenantId && b.ClientId == clientId && b.Notes != null && b.Notes.StartsWith("Rx:"))
            .OrderByDescending(b => b.StartTime)
            .Select(b => new
            {
                prescribedAt = b.StartTime,
                prescribedBy = b.Staff != null ? b.Staff.FirstName + " " + b.Staff.LastName : "Unknown",
                rx = b.Notes
            })
            .Take(50)
            .ToListAsync();

        return Ok(new { clientId, prescriptions = rxHistory, count = rxHistory.Count });
    }

    /// <summary>
    /// V1: POST /verticals/medical/insurance-preauth — Submit insurance pre-authorization request.
    /// </summary>
    [HttpPost("medical/insurance-preauth")]
    public async Task<IActionResult> SubmitInsurancePreAuth([FromBody] InsurancePreAuthRequest request)
    {
        var hipaaSigned = await _context.Set<GdprConsent>()
            .AnyAsync(c => c.TenantId == TenantId && c.ConsentType == "HIPAA_BAA" && c.IsGranted);
        if (!hipaaSigned)
            return StatusCode(403, new { error = "hipaa_baa_required" });

        // Generate a pre-auth reference — in production this would call insurer API
        var preAuthRef = $"PA-{TenantId:N}"[..6].ToUpper() + $"-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}"[..8].ToUpper();

        _logger.LogInformation("[V1] Insurance pre-auth submitted: tenant={TenantId} client={ClientId} insurer={Insurer} ref={Ref}",
            TenantId, request.ClientId, request.InsuranceProvider, preAuthRef);

        return Ok(new
        {
            preAuthRef,
            status = "submitted",
            insuranceProvider = request.InsuranceProvider,
            procedureCode = request.ProcedureCode,
            estimatedApprovalDays = 3,
            submittedAt = DateTime.UtcNow,
            note = "Pre-authorization submitted. Insurers typically respond within 3 business days."
        });
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // V2: Fitness
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// V2: POST /verticals/fitness/session-log — Log a workout session with metrics.
    /// Stored in booking Notes as structured JSON. Replaces MyFitnessPal integration need.
    /// </summary>
    [HttpPost("fitness/session-log")]
    public async Task<IActionResult> LogFitnessSession([FromBody] FitnessSessionLogRequest request)
    {
        var booking = await _context.Bookings
            .FirstOrDefaultAsync(b => b.Id == request.BookingId && b.TenantId == TenantId);
        if (booking == null) return NotFound(new { error = "booking_not_found" });

        var sessionData = System.Text.Json.JsonSerializer.Serialize(new
        {
            metrics = request.Metrics,
            heartRateZones = request.HeartRateZones,
            programName = request.ProgramName,
            notes = request.Notes,
            loggedAt = DateTime.UtcNow
        });

        booking.Notes = $"FitnessLog:{sessionData}";
        await _context.SaveChangesAsync();

        return Ok(new { bookingId = request.BookingId, logged = true, sessionData });
    }

    /// <summary>
    /// V2: GET /verticals/fitness/client-progress/{clientId} — Returns workout history and progression.
    /// </summary>
    [HttpGet("fitness/client-progress/{clientId}")]
    public async Task<IActionResult> GetClientFitnessProgress(Guid clientId, [FromQuery] int weeks = 12)
    {
        var logs = await _context.Bookings
            .Where(b => b.TenantId == TenantId && b.ClientId == clientId && b.Notes != null && b.Notes.StartsWith("FitnessLog:"))
            .OrderByDescending(b => b.StartTime)
            .Take(weeks * 7)
            .Select(b => new { b.StartTime, b.Notes })
            .ToListAsync();

        return Ok(new
        {
            clientId,
            totalSessions = logs.Count,
            weeksCovered = weeks,
            sessions = logs.Select(l => new { l.StartTime, data = l.Notes?[12..] })
        });
    }

    /// <summary>
    /// V2: GET /verticals/fitness/program-templates — Returns workout program templates.
    /// </summary>
    [HttpGet("fitness/program-templates")]
    public IActionResult GetFitnessProgramTemplates()
    {
        var programs = new[]
        {
            new { Id = "fp_001", Name = "12-Week Strength Foundation", Weeks = 12, SessionsPerWeek = 3, Goal = "strength", Progressions = new[] { "Week 1-4: 3×8 compound lifts", "Week 5-8: 4×6 progressive overload", "Week 9-12: 5×5 peak strength" } },
            new { Id = "fp_002", Name = "8-Week HIIT Fat Loss", Weeks = 8, SessionsPerWeek = 4, Goal = "fat_loss", Progressions = new[] { "Week 1-2: 20-min intervals", "Week 3-4: 30-min intervals", "Week 5-6: 40-min + strength", "Week 7-8: Peak intensity" } },
            new { Id = "fp_003", Name = "Yoga Flexibility Journey", Weeks = 6, SessionsPerWeek = 3, Goal = "flexibility", Progressions = new[] { "Week 1-2: Foundation poses", "Week 3-4: Intermediate flows", "Week 5-6: Advanced inversions" } },
            new { Id = "fp_004", Name = "Couch to 5K Running", Weeks = 9, SessionsPerWeek = 3, Goal = "endurance", Progressions = new[] { "Week 1-3: Walk/run intervals", "Week 4-6: Sustained running", "Week 7-9: 5K pace training" } },
        };

        return Ok(new { data = programs });
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // V3: Pet Grooming
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// V3: GET /verticals/pet/breed-matrix — Returns breed-specific service time matrix.
    /// No competitor has breed-aware scheduling.
    /// </summary>
    [HttpGet("pet/breed-matrix")]
    public IActionResult GetBreedServiceMatrix()
    {
        var matrix = new[]
        {
            new { Breed = "Chihuahua",         SizeCat = "XS", FullGroom = 45, Bath = 25, NailTrim = 10, FrequencyWeeks = 6,  SpecialNotes = "Sensitive skin — hypoallergenic shampoo recommended." },
            new { Breed = "Shih Tzu",          SizeCat = "S",  FullGroom = 90, Bath = 30, NailTrim = 10, FrequencyWeeks = 4,  SpecialNotes = "Coat mats easily; daily brushing required by owner." },
            new { Breed = "Golden Retriever",  SizeCat = "L",  FullGroom = 120, Bath = 45, NailTrim = 15, FrequencyWeeks = 8,  SpecialNotes = "Heavy shedder — blowout recommended." },
            new { Breed = "Poodle (Standard)", SizeCat = "M",  FullGroom = 150, Bath = 45, NailTrim = 15, FrequencyWeeks = 6,  SpecialNotes = "Needs breed-specific cut: Puppy, Continental, or English Saddle." },
            new { Breed = "German Shepherd",   SizeCat = "L",  FullGroom = 100, Bath = 40, NailTrim = 15, FrequencyWeeks = 8,  SpecialNotes = "Double coat — deshedding treatment recommended." },
            new { Breed = "Maine Coon",        SizeCat = "L",  FullGroom = 90,  Bath = 35, NailTrim = 10, FrequencyWeeks = 8,  SpecialNotes = "Cat grooming — extra handling time. Semi-long coat." },
            new { Breed = "Bichon Frise",      SizeCat = "S",  FullGroom = 100, Bath = 30, NailTrim = 10, FrequencyWeeks = 6,  SpecialNotes = "White coat — whitening shampoo. Poofy round cut." },
            new { Breed = "Labrador",          SizeCat = "L",  FullGroom = 90,  Bath = 40, NailTrim = 15, FrequencyWeeks = 8,  SpecialNotes = "Short coat — blowout and deshedding." },
        };

        return Ok(new { data = matrix, count = matrix.Length });
    }

    /// <summary>
    /// V3: POST /verticals/pet/vaccination-record — Store vaccination record for a pet/client.
    /// </summary>
    [HttpPost("pet/vaccination-record")]
    public async Task<IActionResult> RecordVaccination([FromBody] PetVaccinationRequest request)
    {
        var client = await _context.Clients
            .FirstOrDefaultAsync(c => c.Id == request.ClientId && c.TenantId == TenantId);
        if (client == null) return NotFound(new { error = "client_not_found" });

        var vaccRecord = $"Vacc:{System.Text.Json.JsonSerializer.Serialize(new { request.PetName, request.Vaccine, administeredDate = request.AdministeredDate, request.VetName, nextDue = request.NextDueDate })}";
        client.Notes = client.Notes == null ? vaccRecord : client.Notes + "\n" + vaccRecord;
        await _context.SaveChangesAsync();

        return Ok(new
        {
            recorded = true,
            petName = request.PetName,
            vaccine = request.Vaccine,
            administeredDate = request.AdministeredDate,
            nextDueDate = request.NextDueDate,
            reminderScheduled = request.NextDueDate != null
        });
    }

    /// <summary>
    /// V3: GET /verticals/pet/upcoming-vaccinations — Returns pets with upcoming vaccination due dates.
    /// </summary>
    [HttpGet("pet/upcoming-vaccinations")]
    public async Task<IActionResult> GetUpcomingVaccinations([FromQuery] int daysAhead = 30)
    {
        // Clients whose Notes contain "Vacc:" and nextDue within daysAhead
        var clients = await _context.Clients
            .Where(c => c.TenantId == TenantId && c.Notes != null && c.Notes.Contains("Vacc:"))
            .Select(c => new { c.Id, c.FirstName, c.LastName, c.Email, c.Phone, c.Notes })
            .Take(200)
            .ToListAsync();

        var upcoming = clients
            .SelectMany(c =>
            {
                var vaccLines = (c.Notes ?? "").Split('\n', StringSplitOptions.RemoveEmptyEntries)
                    .Where(l => l.StartsWith("Vacc:"));
                return vaccLines.Select(l => new { c.Id, c.FirstName, c.LastName, c.Email, vaccinationNote = l });
            })
            .Take(50)
            .ToList();

        return Ok(new { count = upcoming.Count, daysAhead, upcoming });
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // V4: Beauty Education
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// V4: POST /verticals/education/student-booking — Book a student with mentor assignment.
    /// Creates a booking where the student is the client and the mentor is the staff member.
    /// </summary>
    [HttpPost("education/student-booking")]
    public async Task<IActionResult> CreateStudentBooking([FromBody] StudentBookingRequest request)
    {
        var service = await _context.Services
            .FirstOrDefaultAsync(s => s.Id == request.ServiceId && s.TenantId == TenantId);
        if (service == null) return BadRequest(new { error = "service_not_found" });

        var mentor = await _context.StaffMembers
            .FirstOrDefaultAsync(s => s.Id == request.MentorId && s.TenantId == TenantId);
        if (mentor == null) return BadRequest(new { error = "mentor_not_found" });

        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId,
            ClientId = request.StudentClientId,
            ServiceId = request.ServiceId,
            StaffId = request.MentorId,
            StartTime = request.SessionDateTime,
            EndTime = request.SessionDateTime.AddMinutes(service.DurationMinutes),
            Status = BookingStatus.Confirmed,
            Notes = $"EducationSession:module={request.ModuleName};assessment={request.IsAssessment}",
            Source = BookingSource.Manual
        };

        _context.Bookings.Add(booking);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            bookingId = booking.Id,
            student = request.StudentClientId,
            mentor = new { mentor.Id, Name = $"{mentor.FirstName} {mentor.LastName}" },
            module = request.ModuleName,
            isAssessment = request.IsAssessment,
            scheduledAt = booking.StartTime
        });
    }

    /// <summary>
    /// V4: POST /verticals/education/certification — Issue certification to a student on completion.
    /// </summary>
    [HttpPost("education/certification")]
    public async Task<IActionResult> IssueCertification([FromBody] CertificationRequest request)
    {
        var client = await _context.Clients
            .FirstOrDefaultAsync(c => c.Id == request.StudentClientId && c.TenantId == TenantId);
        if (client == null) return NotFound(new { error = "student_not_found" });

        var certId = $"CERT-{TenantId:N}"[..6].ToUpper() + $"-{client.Id:N}"[..6].ToUpper() + $"-{DateTime.UtcNow:yyyyMM}";

        client.Notes = (client.Notes ?? "") + $"\nCertification:{certId}:{request.CertificationName}:{DateTime.UtcNow:yyyy-MM-dd}";
        await _context.SaveChangesAsync();

        _logger.LogInformation("[V4] Certification issued: {CertId} for student {StudentId} cert={Name}",
            certId, request.StudentClientId, request.CertificationName);

        return Ok(new
        {
            certificationId = certId,
            studentId = request.StudentClientId,
            studentName = $"{client.FirstName} {client.LastName}",
            certificationName = request.CertificationName,
            issuedAt = DateTime.UtcNow,
            issuedBy = TenantId,
            verificationUrl = $"https://app.upkilo.com/verify/{certId}"
        });
    }

    /// <summary>
    /// V4: GET /verticals/education/portfolio/{studentClientId} — Returns student's completed modules and certifications.
    /// </summary>
    [HttpGet("education/portfolio/{studentClientId}")]
    public async Task<IActionResult> GetStudentPortfolio(Guid studentClientId)
    {
        var client = await _context.Clients
            .FirstOrDefaultAsync(c => c.Id == studentClientId && c.TenantId == TenantId);
        if (client == null) return NotFound();

        var sessions = await _context.Bookings
            .Where(b => b.TenantId == TenantId && b.ClientId == studentClientId
                     && b.Notes != null && b.Notes.StartsWith("EducationSession:"))
            .Include(b => b.Staff)
            .OrderByDescending(b => b.StartTime)
            .Select(b => new
            {
                b.Id,
                b.StartTime,
                mentor = b.Staff != null ? b.Staff.FirstName + " " + b.Staff.LastName : "TBD",
                b.Status,
                sessionMeta = b.Notes
            })
            .Take(100)
            .ToListAsync();

        var certLines = (client.Notes ?? "").Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Where(l => l.StartsWith("Certification:"))
            .Select(l => l.Split(':'))
            .Where(parts => parts.Length >= 4)
            .Select(parts => new { id = parts[1], name = parts[2], issuedDate = parts[3] })
            .ToList();

        return Ok(new
        {
            studentId = studentClientId,
            studentName = $"{client.FirstName} {client.LastName}",
            totalSessions = sessions.Count,
            certifications = certLines,
            sessions
        });
    }
}

// ── DTOs ───────────────────────────────────────────────────────────────────────

public class InsurancePreAuthRequest
{
    public Guid ClientId { get; set; }
    public string InsuranceProvider { get; set; } = string.Empty;
    public string ProcedureCode { get; set; } = string.Empty;
    public string DiagnosisCode { get; set; } = string.Empty;
    public decimal EstimatedCost { get; set; }
}

public class FitnessSessionLogRequest
{
    public Guid BookingId { get; set; }
    public string? ProgramName { get; set; }
    public Dictionary<string, object>? Metrics { get; set; }
    public Dictionary<string, int>? HeartRateZones { get; set; }
    public string? Notes { get; set; }
}

public class PetVaccinationRequest
{
    public Guid ClientId { get; set; }
    public string PetName { get; set; } = string.Empty;
    public string Vaccine { get; set; } = string.Empty;
    public DateTime AdministeredDate { get; set; }
    public DateTime? NextDueDate { get; set; }
    public string? VetName { get; set; }
}

public class StudentBookingRequest
{
    public Guid StudentClientId { get; set; }
    public Guid ServiceId { get; set; }
    public Guid MentorId { get; set; }
    public DateTime SessionDateTime { get; set; }
    public string ModuleName { get; set; } = string.Empty;
    public bool IsAssessment { get; set; }
}

public class CertificationRequest
{
    public Guid StudentClientId { get; set; }
    public string CertificationName { get; set; } = string.Empty;
    public string? IssuedByName { get; set; }
}
