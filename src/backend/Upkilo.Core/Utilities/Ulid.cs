using System;
using System.Security.Cryptography;

namespace Upkilo.Core.Utilities;

/// <summary>
/// ULID (Universally Unique Lexicographically Sortable Identifier) implementation
/// </summary>
public static class Ulid
{
    private static readonly char[] Encoding = "0123456789ABCDEFGHJKMNPQRSTVWXYZ".ToCharArray();

    public static string NewUlid()
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var random = new byte[10];
        RandomNumberGenerator.Fill(random);

        var chars = new char[26];

        // Timestamp (48 bits -> 10 chars)
        for (int i = 9; i >= 0; i--)
        {
            chars[i] = Encoding[timestamp % 32];
            timestamp /= 32;
        }

        // Randomness (80 bits -> 16 chars)
        for (int i = 0; i < 16; i++)
        {
            chars[i + 10] = Encoding[random[i % 10] % 32];
        }

        return new string(chars);
    }
}
