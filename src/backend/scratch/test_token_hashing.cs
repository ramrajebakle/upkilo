using System;
using System.Security.Cryptography;
using System.Text;
using System.Linq;

public class Program
{
    public static void Main()
    {
        string rawToken = "ABC_DEF-GHI123";
        
        string hash1 = HashToken(rawToken);
        string hash2 = HashToken(rawToken);
        
        Console.WriteLine($"Raw Token: {rawToken}");
        Console.WriteLine($"Hash 1: {hash1}");
        Console.WriteLine($"Hash 2: {hash2}");
        Console.WriteLine($"Match: {hash1 == hash2}");
        
        // Test with different padding-like characters
        string tokenWithPaddingChars = "ABC+DEF/GHI=";
        string hashWithPaddingChars = HashToken(tokenWithPaddingChars);
        Console.WriteLine($"Token with +, /, =: {tokenWithPaddingChars}");
        Console.WriteLine($"Hash: {hashWithPaddingChars}");
    }

    private static string HashToken(string token)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(token);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }
}
