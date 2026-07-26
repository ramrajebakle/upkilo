namespace Upkilo.Core.Messages;

public interface ISmsMessage
{
    Guid TenantId { get; }
    string ToNumber { get; }
    string Body { get; }
    string FromNumber { get; }
}

public record SendSmsEvent(
    Guid TenantId,
    string ToNumber,
    string Body,
    string FromNumber
) : ISmsMessage;
