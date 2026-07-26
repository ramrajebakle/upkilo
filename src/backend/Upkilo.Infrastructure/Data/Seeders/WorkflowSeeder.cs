using Microsoft.EntityFrameworkCore;
using Upkilo.Core.Entities;
using System.Text.Json;

namespace Upkilo.Infrastructure.Data.Seeders;

public static class WorkflowSeeder
{
    public static async Task SeedAsync(AppDbContext context, Guid tenantId)
    {
        // Only seed if no templates exist for this tenant
        if (await context.WorkflowTemplates.IgnoreQueryFilters().AnyAsync(wt => wt.TenantId == tenantId))
        {
            return;
        }

        var templates = new List<WorkflowTemplate>
        {
            new WorkflowTemplate
            {
                TenantId = tenantId,
                Name = "Client Auto-Onboarding",
                Description = "Welcome sequence triggered when a new client registers.",
                TriggerType = "Event",
                TriggerConfig = JsonSerializer.Serialize(new { EventType = "ClientCreated" }),
                Category = "onboarding",
                IsPublic = true,
                Steps = JsonSerializer.Serialize(new object[] 
                {
                    new { Type = "SendEmail", Template = "WelcomeEmail" },
                    new { Type = "Wait", Duration = "3d" },
                    new { Type = "SendEmail", Template = "ProfileCompleteReminder" }
                })
            },
            new WorkflowTemplate
            {
                TenantId = tenantId,
                Name = "Booking Lifecycle & Reminders",
                Description = "Standard flow for a new booking: Confirmation, 24h Reminder, Follow-up.",
                TriggerType = "Event",
                TriggerConfig = JsonSerializer.Serialize(new { EventType = "BookingCreated" }),
                Category = "booking",
                IsPublic = true,
                Steps = JsonSerializer.Serialize(new object[]
                {
                    new { Type = "SendEmail", Template = "BookingConfirmation" },
                    new { Type = "WaitUntil", Expression = "Booking.StartTime - 24h" },
                    new { Type = "SendSms", Template = "BookingReminder24h" },
                    new { Type = "WaitUntil", Expression = "Booking.EndTime + 2h" },
                    new { Type = "SendEmail", Template = "FeedbackRequest" }
                })
            },
            new WorkflowTemplate
            {
                TenantId = tenantId,
                Name = "No-Show Recovery Strategy",
                Description = "Follow-up sequence to re-engage clients who missed their appointment.",
                TriggerType = "Event",
                TriggerConfig = JsonSerializer.Serialize(new { EventType = "BookingNoShow" }),
                Category = "retention",
                IsPublic = true,
                Steps = JsonSerializer.Serialize(new object[]
                {
                    new { Type = "SendEmail", Template = "MissedYouEmail" },
                    new { Type = "Wait", Duration = "7d" },
                    new { Type = "SendSms", Template = "ReBookDiscountSms" }
                })
            },
            new WorkflowTemplate
            {
                TenantId = tenantId,
                Name = "Staff Onboarding & Compliance",
                Description = "Triggered when a new staff member is added to gather documents.",
                TriggerType = "Event",
                TriggerConfig = JsonSerializer.Serialize(new { EventType = "StaffCreated" }),
                Category = "hr",
                IsPublic = true,
                Steps = JsonSerializer.Serialize(new object[]
                {
                    new { Type = "SendEmail", Template = "StaffWelcomeEmail" },
                    new { Type = "Wait", Duration = "1d" },
                    new { Type = "CreateTask", Assignee = "Manager", Title = "Verify Compliance Documents" }
                })
            },
            new WorkflowTemplate
            {
                TenantId = tenantId,
                Name = "Abandoned Booking Recovery",
                Description = "Follow-up sequence when a booking payment fails or checkout is abandoned.",
                TriggerType = "Event",
                TriggerConfig = JsonSerializer.Serialize(new { EventType = "PaymentFailed" }),
                Category = "retention",
                IsPublic = true,
                Steps = JsonSerializer.Serialize(new object[]
                {
                    new { Type = "SendEmail", Template = "AbandonedCheckoutRecovery" },
                    new { Type = "Wait", Duration = "1d" },
                    new { Type = "SendSms", Template = "CheckoutOfferSms" }
                })
            },
            new WorkflowTemplate
            {
                TenantId = tenantId,
                Name = "Loyalty Milestone Celebration",
                Description = "Automated reward and note added when a booking is completed.",
                TriggerType = "Event",
                TriggerConfig = JsonSerializer.Serialize(new { EventType = "BookingCompleted" }),
                Category = "marketing",
                IsPublic = true,
                Steps = JsonSerializer.Serialize(new object[]
                {
                    new { Type = "SendEmail", Template = "MilestoneThankYou" },
                    new { Type = "CreateTask", Assignee = "Manager", Title = "Send Loyalty Bonus Pack" },
                    new { Type = "AddClientNote", Content = "Loyalty milestone achieved on booking completion." }
                })
            },
            new WorkflowTemplate
            {
                TenantId = tenantId,
                Name = "Client Feedback Loop",
                Description = "Collect and track review submissions automatically.",
                TriggerType = "Event",
                TriggerConfig = JsonSerializer.Serialize(new { EventType = "ReviewSubmitted" }),
                Category = "marketing",
                IsPublic = true,
                Steps = JsonSerializer.Serialize(new object[]
                {
                    new { Type = "CreateTask", Title = "Review Feedback Action", Priority = "High" },
                    new { Type = "SendEmail", Template = "FeedbackAcknowledgement" }
                })
            },
            new WorkflowTemplate
            {
                TenantId = tenantId,
                Name = "Refund & Retention Flow",
                Description = "Customer care and follow-up triggered by refunds.",
                TriggerType = "Event",
                TriggerConfig = JsonSerializer.Serialize(new { EventType = "RefundIssued" }),
                Category = "retention",
                IsPublic = true,
                Steps = JsonSerializer.Serialize(new object[]
                {
                    new { Type = "AddClientNote", Content = "Refund processed, checking customer satisfaction." },
                    new { Type = "SendEmail", Template = "RefundFollowUp" }
                })
            }
        };

        context.WorkflowTemplates.AddRange(templates);
        await context.SaveChangesAsync();
    }
}
