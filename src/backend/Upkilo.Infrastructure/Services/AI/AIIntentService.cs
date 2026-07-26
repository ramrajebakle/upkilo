using System.Text.Json;
using Microsoft.Extensions.Logging;
using Upkilo.Core.Interfaces;

namespace Upkilo.Infrastructure.Services.AI;

public interface IAIIntentService
{
    Task<string> ParseIntentAsync(string userMessage, Guid tenantId);
}

public class AIIntentService : IAIIntentService
{
    private readonly ILogger<AIIntentService> _logger;
    private readonly IAIService _aiService;

    public AIIntentService(ILogger<AIIntentService> logger, IAIService aiService)
    {
        _logger = logger;
        _aiService = aiService;
    }

    public async Task<string> ParseIntentAsync(string userMessage, Guid tenantId)
    {
        var prompt = $"Analyze the user message and identify their booking intent. " +
                     $"Choose exactly one of the following intent names:\n" +
                     $"- ModifyBooking (if they want to cancel, reschedule, change their appointment)\n" +
                     $"- BookAppointment (if they want to book or create an appointment/booking)\n" +
                     $"- InquirePricing (if they ask about prices, costs, fees, how much)\n" +
                     $"- InquireLocation (if they ask about location, address, where it is)\n" +
                     $"- UnknownIntent (if it doesn't match any of the above)\n\n" +
                     $"User message: \"{userMessage}\"\n\n" +
                     $"Output only the intent name (exactly as written above, with no extra text, explanations, or quotes).";

        try
        {
            var result = await _aiService.GenerateTextAsync(tenantId, null, prompt, "gpt-4");
            if (result.Success && !string.IsNullOrWhiteSpace(result.Content))
            {
                var intent = result.Content.Trim().Replace("\"", "").Replace("'", "");
                var validIntents = new HashSet<string> { "ModifyBooking", "BookAppointment", "InquirePricing", "InquireLocation", "UnknownIntent" };
                if (validIntents.Contains(intent))
                {
                    _logger.LogInformation("Successfully parsed AI intent: {Intent} for message: {Message}", intent, userMessage);
                    return intent;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse intent using AI. Falling back to heuristics.");
        }

        // Fallback to heuristics
        var msgLower = userMessage.ToLower();
        if (msgLower.Contains("cancel") || msgLower.Contains("reschedule"))
            return "ModifyBooking";
        if (msgLower.Contains("book") || msgLower.Contains("appointment"))
            return "BookAppointment";
        if (msgLower.Contains("price") || msgLower.Contains("how much"))
            return "InquirePricing";
        if (msgLower.Contains("where") || msgLower.Contains("location"))
            return "InquireLocation";

        return "UnknownIntent";
    }
}
