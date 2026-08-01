using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.API.Controllers;

/// <summary>
/// Handles gated content, courses, drip delivery, and member progress tracking.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/membership-content")]
[Authorize]
public class MembershipContentController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<MembershipContentController> _logger;

    public MembershipContentController(AppDbContext context, ITenantProvider tenantProvider, ILogger<MembershipContentController> logger)
    {
        _context = context;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    // ── Admin: Content Management ─────────────────────────────────────────────

    [HttpGet("admin/courses")]
    public async Task<IActionResult> GetAdminCourses()
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var courses = await _context.MembershipContents
            .Include(c => c.Modules.Where(m => !m.IsDeleted))
                .ThenInclude(m => m.Lessons.Where(l => !l.IsDeleted))
            .Where(c => c.TenantId == tenantId.Value && !c.IsDeleted)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new
            {
                c.Id,
                c.Title,
                c.Description,
                c.Type,
                c.IsPublished,
                RequiredPlanIds = JsonSerializer.Deserialize<List<Guid>>(c.RequiredPlanIds, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }),
                modulesCount = c.Modules.Count,
                lessonsCount = c.Modules.SelectMany(m => m.Lessons).Count()
            })
            .ToListAsync();

        return Ok(new { data = courses });
    }

    [HttpPost("admin/courses")]
    public async Task<IActionResult> CreateCourse([FromBody] CreateCourseRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var course = new MembershipContent
        {
            TenantId = tenantId.Value,
            Title = request.Title,
            Description = request.Description,
            Type = request.Type,
            IsPublished = request.IsPublished,
            RequiredPlanIds = JsonSerializer.Serialize(request.RequiredPlanIds ?? new List<Guid>())
        };

        _context.MembershipContents.Add(course);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Course created: {Title} by {TenantId}", course.Title, tenantId);
        return Ok(course);
    }

    [HttpPost("admin/modules")]
    public async Task<IActionResult> CreateModule([FromBody] CreateModuleRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var module = new MembershipModule
        {
            TenantId = tenantId.Value,
            MembershipContentId = request.CourseId,
            Title = request.Title,
            Description = request.Description,
            SortOrder = request.SortOrder,
            DripDaysDelay = request.DripDaysDelay
        };

        _context.MembershipModules.Add(module);
        await _context.SaveChangesAsync();
        return Ok(module);
    }

    [HttpPost("admin/lessons")]
    public async Task<IActionResult> CreateLesson([FromBody] CreateLessonRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var lesson = new MembershipLesson
        {
            TenantId = tenantId.Value,
            MembershipModuleId = request.ModuleId,
            Title = request.Title,
            BodyHtml = request.BodyHtml,
            VideoUrl = request.VideoUrl,
            AttachmentUrl = request.AttachmentUrl,
            SortOrder = request.SortOrder,
            DurationMinutes = request.DurationMinutes
        };

        _context.MembershipLessons.Add(lesson);
        await _context.SaveChangesAsync();
        return Ok(lesson);
    }

    // ── Member Area: Gated Content & Progress ───────────────────────────────

    /// <summary>
    /// Gets courses the client is allowed to access based on their active subscriptions
    /// </summary>
    [HttpGet("my-library")]
    public async Task<IActionResult> GetMyLibrary()
    {
        // Require client context (typically pulled from claims in real implementation)
        var clientIdStr = User.FindFirst("client_id")?.Value;
        if (!Guid.TryParse(clientIdStr, out var clientId))
            return Unauthorized(new { error = "Client ID not found in token." });

        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        // Find active memberships for client
        var activeSubPlanIds = await _context.ClientMemberships
            .Where(cm => cm.ClientId == clientId && cm.TenantId == tenantId.Value && !cm.IsDeleted && cm.Status == MembershipStatus.Active)
            .Select(cm => cm.MembershipPlanId)
            .ToListAsync();

        // Get published courses
        var courses = await _context.MembershipContents
            .Include(c => c.Modules)
            .Where(c => c.TenantId == tenantId.Value && !c.IsDeleted && c.IsPublished)
            .ToListAsync();

        // Filter: If course requires plans, client must have at least one of them
        var accessibleCourses = courses.Where(c =>
        {
            var requiredPlans = JsonSerializer.Deserialize<List<Guid>>(c.RequiredPlanIds) ?? new List<Guid>();
            return !requiredPlans.Any() || requiredPlans.Intersect(activeSubPlanIds).Any();
        }).ToList();

        // Get progress for these courses
        var lessonIds = accessibleCourses.SelectMany(c => c.Modules.SelectMany(m => m.Lessons.Select(l => l.Id))).ToList();

        var progress = await _context.ClientContentProgresses
            .Where(p => p.ClientId == clientId && lessonIds.Contains(p.MembershipLessonId))
            .ToListAsync();

        var result = accessibleCourses.Select(c => new
        {
            c.Id,
            c.Title,
            c.Description,
            c.ThumbnailUrl,
            c.Type,
            totalLessons = c.Modules.SelectMany(m => m.Lessons).Count(),
            completedLessons = c.Modules.SelectMany(m => m.Lessons).Count(l => progress.Any(p => p.MembershipLessonId == l.Id && p.IsCompleted))
        });

        return Ok(new { data = result });
    }

    [HttpGet("courses/{courseId}/syllabus")]
    public async Task<IActionResult> GetCourseSyllabus(Guid courseId)
    {
        var clientIdStr = User.FindFirst("client_id")?.Value;
        if (!Guid.TryParse(clientIdStr, out var clientId))
            return Unauthorized();

        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        // Fetch course with modules and lessons
        var course = await _context.MembershipContents
            .Include(c => c.Modules.Where(m => !m.IsDeleted).OrderBy(m => m.SortOrder))
                .ThenInclude(m => m.Lessons.Where(l => !l.IsDeleted).OrderBy(l => l.SortOrder))
            .FirstOrDefaultAsync(c => c.Id == courseId && c.TenantId == tenantId.Value && !c.IsDeleted);

        if (course == null) return NotFound();

        // Drip feature: When did user get access? 
        // We approximate using earliest active subscription start date for now
        var earliestSubDate = await _context.ClientMemberships
            .Where(cm => cm.ClientId == clientId && cm.TenantId == tenantId.Value && cm.Status == MembershipStatus.Active)
            .MinAsync(cm => (DateTime?)cm.StartDate) ?? DateTime.UtcNow;

        var daysSinceEnrollment = (DateTime.UtcNow - earliestSubDate).TotalDays;

        var lessonIds = course.Modules.SelectMany(m => m.Lessons.Select(l => l.Id)).ToList();
        var progressDict = await _context.ClientContentProgresses
            .Where(p => p.ClientId == clientId && lessonIds.Contains(p.MembershipLessonId))
            .ToDictionaryAsync(p => p.MembershipLessonId);

        var modules = course.Modules.Select(m => new
        {
            m.Id,
            m.Title,
            m.Description,
            isLocked = m.DripDaysDelay > daysSinceEnrollment, // Drip content check
            unlocksInDays = m.DripDaysDelay > daysSinceEnrollment ? Math.Ceiling(m.DripDaysDelay - daysSinceEnrollment) : 0,
            lessons = m.Lessons.Select(l => new
            {
                l.Id,
                l.Title,
                l.DurationMinutes,
                l.VideoUrl,
                isCompleted = progressDict.TryGetValue(l.Id, out var p) && p.IsCompleted,
                lastPosition = progressDict.TryGetValue(l.Id, out var p2) ? p2.LastPositionSeconds : 0
            })
        });

        return Ok(new { course.Id, course.Title, course.Description, modules });
    }

    [HttpPost("lessons/{lessonId}/progress")]
    public async Task<IActionResult> UpdateProgress(Guid lessonId, [FromBody] MembershipProgressRequest request)
    {
        var clientIdStr = User.FindFirst("client_id")?.Value;
        if (!Guid.TryParse(clientIdStr, out var clientId))
            return Unauthorized();

        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var progress = await _context.ClientContentProgresses
            .FirstOrDefaultAsync(p => p.ClientId == clientId && p.MembershipLessonId == lessonId && p.TenantId == tenantId.Value);

        if (progress == null)
        {
            progress = new ClientContentProgress
            {
                TenantId = tenantId.Value,
                ClientId = clientId,
                MembershipLessonId = lessonId,
                StartedAt = DateTime.UtcNow,
                LastPositionSeconds = request.PositionSeconds,
                IsCompleted = request.IsCompleted,
                CompletedAt = request.IsCompleted ? DateTime.UtcNow : null
            };
            _context.ClientContentProgresses.Add(progress);
        }
        else
        {
            progress.LastPositionSeconds = request.PositionSeconds;
            if (request.IsCompleted && !progress.IsCompleted)
            {
                progress.IsCompleted = true;
                progress.CompletedAt = DateTime.UtcNow;
            }
        }

        await _context.SaveChangesAsync();
        return Ok(new { success = true });
    }
}

// ── Request DTOs ──────────────────────────────────────────────────────────────

public record CreateCourseRequest(string Title, string? Description, ContentType Type, bool IsPublished, List<Guid>? RequiredPlanIds);
public record CreateModuleRequest(Guid CourseId, string Title, string? Description, int SortOrder, int DripDaysDelay);
public record CreateLessonRequest(Guid ModuleId, string Title, string? BodyHtml, string? VideoUrl, string? AttachmentUrl, int SortOrder, int DurationMinutes);
public record MembershipProgressRequest(int PositionSeconds, bool IsCompleted);
