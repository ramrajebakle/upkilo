using System.Text.RegularExpressions;
using Serilog.Core;
using Serilog.Events;

namespace Upkilo.API.Middleware;

/// <summary>
/// Serilog enricher that intercepts log events and masks PII
/// (emails, phone numbers) to prevent sensitive data leakage
/// into log sinks (console, files, Application Insights, etc.)
///
/// Usage: .Enrich.With<PiiRedactionEnricher>()
/// </summary>
public partial class PiiRedactionEnricher : ILogEventEnricher
{
    private static readonly string[] SensitiveFieldNames = { "Password", "Secret", "Key", "Token", "ApiKey", "AuthToken", "ClientSecret", "WebhookSecret", "ConnectionString" };

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var properties = logEvent.Properties.ToList();
        foreach (var prop in properties)
        {
            var redactedValue = RedactLogEventPropertyValue(prop.Key, prop.Value, propertyFactory);
            if (redactedValue != prop.Value)
            {
                logEvent.AddOrUpdateProperty(
                    propertyFactory.CreateProperty(prop.Key, redactedValue));
            }
        }
    }

    private LogEventPropertyValue RedactLogEventPropertyValue(string key, LogEventPropertyValue value, ILogEventPropertyFactory propertyFactory)
    {
        // 1. Scalar strings
        if (value is ScalarValue scalar && scalar.Value is string strValue)
        {
            // If the key itself is sensitive, redact completely
            if (SensitiveFieldNames.Any(s => key.Contains(s, StringComparison.OrdinalIgnoreCase)))
            {
                return new ScalarValue("[REDACTED]");
            }

            var redacted = RedactPii(strValue);
            return redacted != strValue ? new ScalarValue(redacted) : value;
        }

        // 2. Structured objects (Recursive)
        if (value is StructureValue structure)
        {
            var newProperties = new List<LogEventProperty>();
            bool changed = false;

            foreach (var prop in structure.Properties)
            {
                var redacted = RedactLogEventPropertyValue(prop.Name, prop.Value, propertyFactory);
                if (redacted != prop.Value)
                {
                    newProperties.Add(new LogEventProperty(prop.Name, redacted));
                    changed = true;
                }
                else
                {
                    newProperties.Add(prop);
                }
            }

            return changed ? new StructureValue(newProperties, structure.TypeTag) : value;
        }

        // 3. Sequences (Recursive)
        if (value is SequenceValue sequence)
        {
            var newElements = new List<LogEventPropertyValue>();
            bool changed = false;

            foreach (var element in sequence.Elements)
            {
                var redacted = RedactLogEventPropertyValue(key, element, propertyFactory);
                if (redacted != element)
                {
                    newElements.Add(redacted);
                    changed = true;
                }
                else
                {
                    newElements.Add(element);
                }
            }

            return changed ? new SequenceValue(newElements) : value;
        }

        return value;
    }

    private static string RedactPii(string input)
    {
        return Upkilo.Core.Helpers.PiiHelper.RedactPii(input);
    }
}
