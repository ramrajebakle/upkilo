using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Upkilo.API.Attributes;

namespace Upkilo.API.Controllers;

[ApiController]
[Route("api/workflows/templates")]
[Authorize]
[FeatureGuard("ai_workflows")]
public class WorkflowTemplatesController : ControllerBase
{
    private readonly ILogger<WorkflowTemplatesController> _logger;

    public WorkflowTemplatesController(ILogger<WorkflowTemplatesController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Get all pre-built workflow templates
    /// </summary>
    [HttpGet]
    public IActionResult GetTemplates()
    {
        return Ok(new WorkflowTemplate[]
        {
            new WorkflowTemplate
            {
                Id = "welcome-series",
                Name = "Welcome Series",
                Description = "Send a series of welcome emails to new clients",
                Category = "Onboarding",
                Trigger = "client.created",
                Steps = new object[]
                {
                    new { action = "send_email", template = "welcome", delay = "0" },
                    new { action = "wait", duration = "24h" },
                    new { action = "send_email", template = "getting_started", delay = "0" },
                    new { action = "wait", duration = "72h" },
                    new { action = "send_email", template = "book_first_appointment", delay = "0" }
                }
            },
            new WorkflowTemplate
            {
                Id = "appointment-reminder",
                Name = "Appointment Reminder",
                Description = "Send reminders before appointments",
                Category = "Booking",
                Trigger = "booking.created",
                Steps = new object[]
                {
                    new { action = "wait_until", before = "24h" },
                    new { action = "send_email", template = "reminder_24h" },
                    new { action = "send_sms", template = "reminder_24h" },
                    new { action = "wait_until", before = "2h" },
                    new { action = "send_sms", template = "reminder_2h" }
                }
            },
            new WorkflowTemplate
            {
                Id = "no-show-recovery",
                Name = "No-Show Recovery",
                Description = "Re-engage clients who missed their appointment",
                Category = "Recovery",
                Trigger = "booking.no_show",
                Steps = new object[]
                {
                    new { action = "wait", duration = "1h" },
                    new { action = "send_email", template = "missed_appointment" },
                    new { action = "wait", duration = "24h" },
                    new { action = "send_sms", template = "rebook_offer" }
                }
            },
            new WorkflowTemplate
            {
                Id = "review-request",
                Name = "Review Request",
                Description = "Request reviews after completed appointments",
                Category = "Marketing",
                Trigger = "booking.completed",
                Steps = new object[]
                {
                    new { action = "wait", duration = "2h" },
                    new { action = "send_email", template = "thank_you" },
                    new { action = "wait", duration = "24h" },
                    new { action = "send_email", template = "review_request" }
                }
            },
            new WorkflowTemplate
            {
                Id = "birthday-campaign",
                Name = "Birthday Campaign",
                Description = "Send birthday wishes and special offers",
                Category = "Marketing",
                Trigger = "client.birthday",
                Steps = new object[]
                {
                    new { action = "send_email", template = "birthday_offer" },
                    new { action = "send_sms", template = "birthday_wishes" }
                }
            },
            new WorkflowTemplate
            {
                Id = "re-engagement",
                Name = "Re-engagement Campaign",
                Description = "Win back inactive clients",
                Category = "Marketing",
                Trigger = "client.inactive_30d",
                Steps = new object[]
                {
                    new { action = "send_email", template = "we_miss_you" },
                    new { action = "wait", duration = "7d" },
                    new { action = "condition", if_no_booking = true },
                    new { action = "send_email", template = "special_offer" },
                    new { action = "wait", duration = "7d" },
                    new { action = "send_sms", template = "last_chance_offer" }
                }
            },
            new WorkflowTemplate
            {
                Id = "payment-confirmation",
                Name = "Payment Confirmation",
                Description = "Send payment receipts and confirmations",
                Category = "Transactional",
                Trigger = "payment.received",
                Steps = new object[]
                {
                    new { action = "send_email", template = "payment_receipt" }
                }
            },
            new WorkflowTemplate
            {
                Id = "cancellation-followup",
                Name = "Cancellation Follow-up",
                Description = "Follow up on cancelled appointments",
                Category = "Recovery",
                Trigger = "booking.cancelled",
                Steps = new object[]
                {
                    new { action = "wait", duration = "1h" },
                    new { action = "send_email", template = "sorry_to_see_you_go" },
                    new { action = "wait", duration = "24h" },
                    new { action = "send_email", template = "rebook_suggestion" }
                }
            }
        });
    }

    /// <summary>
    /// Get a specific template by ID
    /// </summary>
    [HttpGet("{id}")]
    public IActionResult GetTemplate(string id)
    {
        var templates = (GetTemplates() as OkObjectResult)?.Value as WorkflowTemplate[];
        var template = templates?.FirstOrDefault(t => t.Id == id);

        if (template == null)
            return NotFound(new { error = "Template not found" });

        return Ok(template);
    }

    /// <summary>
    /// Clone a template to create a new workflow
    /// </summary>
    [HttpPost("{id}/clone")]
    public IActionResult CloneTemplate(string id, [FromBody] CloneTemplateRequest request)
    {
        _logger.LogInformation("Cloning workflow template {TemplateId} as {Name}", id, request.Name);

        return Ok(new
        {
            id = Guid.NewGuid(),
            name = request.Name,
            templateId = id,
            status = "draft",
            message = "Workflow created from template. Edit and activate when ready."
        });
    }
}

public class WorkflowTemplate
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Trigger { get; set; } = string.Empty;
    public object[] Steps { get; set; } = Array.Empty<object>();
}

public record CloneTemplateRequest(string Name);
