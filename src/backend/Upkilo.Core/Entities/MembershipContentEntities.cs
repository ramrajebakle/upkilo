using System;
using System.Collections.Generic;

namespace Upkilo.Core.Entities;

/// <summary>
/// A top-level container for gated content (e.g., a Course, an E-Book, a Resource Library)
/// </summary>
public class MembershipContent : TenantEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ThumbnailUrl { get; set; }
    public ContentType Type { get; set; } = ContentType.Course;
    public bool IsPublished { get; set; }
    
    // JSON array of Guid strings (MembershipPlan IDs) that grant access to this content
    public string RequiredPlanIds { get; set; } = "[]"; 

    public virtual ICollection<MembershipModule> Modules { get; set; } = new List<MembershipModule>();
}

/// <summary>
/// A section or module within a MembershipContent (e.g., "Week 1", "Getting Started")
/// </summary>
public class MembershipModule : TenantEntity
{
    public Guid MembershipContentId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    
    // Drip content functionality: unlock module X days after enrollment
    public int DripDaysDelay { get; set; } 

    public virtual MembershipContent? Content { get; set; }
    public virtual ICollection<MembershipLesson> Lessons { get; set; } = new List<MembershipLesson>();
}

/// <summary>
/// An individual piece of content (video, PDF, text lesson)
/// </summary>
public class MembershipLesson : TenantEntity
{
    public Guid MembershipModuleId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? BodyHtml { get; set; }
    public string? VideoUrl { get; set; }
    public string? AttachmentUrl { get; set; }
    public int SortOrder { get; set; }
    public int DurationMinutes { get; set; } // Estimated time to complete

    public virtual MembershipModule? Module { get; set; }
}

/// <summary>
/// Tracks a client's progression through content
/// </summary>
public class ClientContentProgress : TenantEntity
{
    public Guid ClientId { get; set; }
    public Guid MembershipLessonId { get; set; }
    
    // When the user started the lesson
    public DateTime StartedAt { get; set; }
    
    // Last position (e.g., video timestamp in seconds)
    public int LastPositionSeconds { get; set; }
    
    public bool IsCompleted { get; set; }
    public DateTime? CompletedAt { get; set; }

    public virtual Client? Client { get; set; }
    public virtual MembershipLesson? Lesson { get; set; }
}

public enum ContentType
{
    Course,
    Downloadable,
    VideoSeries,
    CommunityGroup
}
