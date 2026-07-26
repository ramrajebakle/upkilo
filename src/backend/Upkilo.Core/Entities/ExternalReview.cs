using System;
using System.Collections.Generic;

namespace Upkilo.Core.Entities;

/// <summary>
/// Aggregated review from external platforms (Google, Yelp, Facebook).
/// </summary>
public class ExternalReview : TenantEntity
{
    public string Platform { get; set; } = string.Empty; // Google, Yelp, Facebook, Upkilo
    public string ReviewerName { get; set; } = string.Empty;
    public int Rating { get; set; } // 1-5
    public string? ReviewText { get; set; }
    public string? ResponseText { get; set; }
    public DateTime? RespondedAt { get; set; }
    public string? ExternalReviewId { get; set; }
    public string Sentiment { get; set; } = "Neutral"; // Positive, Neutral, Negative
    public DateTime ReviewDate { get; set; }
    public bool IsVerified { get; set; }
    public Guid? ClientId { get; set; }
    public Guid? BookingId { get; set; }
}

/// <summary>
/// Automated review request sent after a booking.
/// </summary>
public class ReviewRequest : TenantEntity
{
    public Guid ClientId { get; set; }
    public Guid? BookingId { get; set; }
    public string Channel { get; set; } = "Email"; // Email, SMS
    public string Status { get; set; } = "Pending"; // Pending, Sent, Completed, Declined
    public string? ReviewUrl { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public virtual Client? Client { get; set; }
}
