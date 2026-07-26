using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Upkilo.Core.Helpers;

public static partial class PiiHelper
{
    // Matches email patterns: user@domain.com
    private static readonly Regex EmailRegex = BuildEmailRegex();

    // Matches phone patterns: +1234567890, (123) 456-7890, 123-456-7890
    private static readonly Regex PhoneRegex = BuildPhoneRegex();

    public static string RedactPii(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        // Mask emails: user@domain.com -> u***@d***.com
        var result = EmailRegex.Replace(input, match =>
        {
            var parts = match.Value.Split('@');
            if (parts.Length != 2) return match.Value;

            var localPart = parts[0].Length > 1
                ? parts[0][0] + "***"
                : "***";

            var domainParts = parts[1].Split('.');
            var domainMasked = domainParts[0].Length > 1
                ? domainParts[0][0] + "***"
                : "***";

            return $"{localPart}@{domainMasked}.{string.Join(".", domainParts.Skip(1))}";
        });

        // Mask phone numbers: +1234567890 -> +1***7890
        result = PhoneRegex.Replace(result, match =>
        {
            var digits = new string(match.Value.Where(char.IsDigit).ToArray());
            if (digits.Length < 4) return "***";
            return digits[..2] + "***" + digits[^4..];
        });

        return result;
    }

    [GeneratedRegex(@"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}", RegexOptions.Compiled)]
    private static partial Regex BuildEmailRegex();

    [GeneratedRegex(@"(\+?\d[\d\s\-().]{8,}\d)", RegexOptions.Compiled)]
    private static partial Regex BuildPhoneRegex();
}
