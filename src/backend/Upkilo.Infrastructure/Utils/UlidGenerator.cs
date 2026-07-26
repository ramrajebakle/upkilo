using System;
using System.Security.Cryptography;
using System.Text;

namespace Upkilo.Infrastructure.Utils;

/// <summary>
/// Utility for generating ULIDs (Universally Unique Lexicographically Sortable Identifiers).
/// </summary>
public static class UlidGenerator
{
    private const string EncodingChars = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";
    
    public static string NewUlid()
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var timestampPart = EncodeTimestamp(timestamp, 10);
        var randomPart = EncodeRandom(16);
        
        return timestampPart + randomPart;
    }

    private static string EncodeTimestamp(long timestamp, int length)
    {
        var buffer = new char[length];
        for (var i = length - 1; i >= 0; i--)
        {
            buffer[i] = EncodingChars[(int)(timestamp % 32)];
            timestamp /= 32;
        }
        return new string(buffer);
    }

    private static string EncodeRandom(int length)
    {
        var bytes = new byte[length];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        
        var buffer = new char[length];
        for (var i = 0; i < length; i++)
        {
            buffer[i] = EncodingChars[bytes[i] % 32];
        }
        return new string(buffer);
    }
}
