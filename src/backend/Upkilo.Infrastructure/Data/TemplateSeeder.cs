using System.Text.Json;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;

namespace Upkilo.Infrastructure.Data
{
    public static class TemplateSeeder
    {
        public static async Task SeedAsync(AppDbContext context)
        {
            if (context.WorkflowTemplates.Any()) return;

            var templates = new List<WorkflowTemplate>
            {
                new WorkflowTemplate
                {
                    Id = Guid.NewGuid(),
                    Name = "Appointment Confirmation",
                    Description = "Sends an immediate email when a new booking is created.",
                    Category = "Booking",
                    TriggerType = "booking.created",
                    TriggerConfig = "{}",
                    IsPublic = true,
                    Steps = JsonSerializer.Serialize(new List<WorkflowStep>
                    {
                        new WorkflowStep
                        {
                            Type = "Action",
                            ActionType = "SendEmail",
                            Config = JsonDocument.Parse(JsonSerializer.Serialize(new
                            {
                                To = "{{ClientEmail}}",
                                Subject = "Booking Confirmed: {{ServiceName}}",
                                Body = "Hi {{FirstName}}, your appointment for {{ServiceName}} is confirmed for {{StartTime}}."
                            })).RootElement
                        }
                    })
                },
                new WorkflowTemplate
                {
                    Id = Guid.NewGuid(),
                    Name = "No-Show Recovery",
                    Description = "Sends a follow-up SMS 30 minutes after a booking is cancelled.",
                    Category = "Retention",
                    TriggerType = "booking.cancelled",
                    TriggerConfig = "{}",
                    IsPublic = true,
                    Steps = JsonSerializer.Serialize(new List<WorkflowStep>
                    {
                        new WorkflowStep
                        {
                            Type = "Wait",
                            Config = JsonDocument.Parse(JsonSerializer.Serialize(new { DelayMinutes = "30" })).RootElement
                        },
                        new WorkflowStep
                        {
                            Type = "Action",
                            ActionType = "SendSms",
                            Config = JsonDocument.Parse(JsonSerializer.Serialize(new
                            {
                                To = "{{Phone}}",
                                Message = "Hi {{FirstName}}, we're sorry you couldn't make it to your {{ServiceName}} appointment. Reply to reschedule!"
                            })).RootElement
                        }
                    })
                },
                new WorkflowTemplate
                {
                    Id = Guid.NewGuid(),
                    Name = "Post-Service Thank You",
                    Description = "Sends an email 2 hours after completion with a review request.",
                    Category = "Marketing",
                    TriggerType = "booking.completed",
                    TriggerConfig = "{}",
                    IsPublic = true,
                    Steps = JsonSerializer.Serialize(new List<WorkflowStep>
                    {
                        new WorkflowStep
                        {
                            Type = "Wait",
                            Config = JsonDocument.Parse(JsonSerializer.Serialize(new { DelayMinutes = "120" })).RootElement
                        },
                        new WorkflowStep
                        {
                            Type = "Action",
                            ActionType = "SendEmail",
                            Config = JsonDocument.Parse(JsonSerializer.Serialize(new
                            {
                                To = "{{ClientEmail}}",
                                Subject = "How was your visit today?",
                                Body = "Hi {{FirstName}}, thanks for visiting us for {{ServiceName}}! We'd love to hear your feedback here: https://review.me/upkilo"
                            })).RootElement
                        }
                    })
                },
                new WorkflowTemplate
                {
                    Id = Guid.NewGuid(),
                    Name = "Auto-Onboarding",
                    Description = "Sends a welcome sequence to new clients created in the system.",
                    Category = "Onboarding",
                    TriggerType = "client.created",
                    TriggerConfig = "{}",
                    IsPublic = true,
                    Steps = JsonSerializer.Serialize(new List<WorkflowStep>
                    {
                        new WorkflowStep
                        {
                            Type = "Action",
                            ActionType = "SendEmail",
                            Config = JsonDocument.Parse(JsonSerializer.Serialize(new
                            {
                                To = "{{ClientEmail}}",
                                Subject = "Welcome to {{BusinessName}}!",
                                Body = "Hi {{FirstName}}, we're thrilled to have you! You can book your first service here: {{BookingLink}}"
                            })).RootElement
                        }
                    })
                },
                new WorkflowTemplate
                {
                    Id = Guid.NewGuid(),
                    Name = "Re-engagement",
                    Description = "Sends a win-back offer to clients who haven't visited in 90 days.",
                    Category = "Retention",
                    TriggerType = "client.inactive_90_days",
                    TriggerConfig = "{}",
                    IsPublic = true,
                    Steps = JsonSerializer.Serialize(new List<WorkflowStep>
                    {
                        new WorkflowStep
                        {
                            Type = "Action",
                            ActionType = "SendEmail",
                            Config = JsonDocument.Parse(JsonSerializer.Serialize(new
                            {
                                To = "{{ClientEmail}}",
                                Subject = "We miss you, {{FirstName}}!",
                                Body = "Hi {{FirstName}}, it's been a while. Use code COMEBACK20 for 20% off your next visit!"
                            })).RootElement
                        }
                    })
                }
            };

            context.WorkflowTemplates.AddRange(templates);
            await context.SaveChangesAsync();
        }
    }
}
