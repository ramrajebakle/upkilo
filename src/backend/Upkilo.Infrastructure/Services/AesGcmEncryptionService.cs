using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Upkilo.Core.Interfaces;

namespace Upkilo.Infrastructure.Services;

/// <summary>
/// AES-256-GCM encryption for tenant credentials stored in the database.
/// Key source: Integration:EncryptionKey in configuration (64 hex chars = 32 bytes).
/// </summary>
public class AesGcmEncryptionService : IEncryptionService
{
    private readonly byte[] _key;
    private const int KeyBytes = 32;  // 256-bit
    private const int NonceBytes = 12; // 96-bit — NIST recommended for GCM
    private const int TagBytes = 16;   // 128-bit authentication tag

    public AesGcmEncryptionService(IConfiguration configuration)
    {
        var keyHex = configuration["Integration:EncryptionKey"];
        if (string.IsNullOrEmpty(keyHex))
            throw new InvalidOperationException(
                "Integration:EncryptionKey is not configured. " +
                "Generate a 64-char hex key and set it in app settings or Key Vault.");

        _key = Convert.FromHexString(keyHex);
        if (_key.Length != KeyBytes)
            throw new InvalidOperationException(
                $"Integration:EncryptionKey must be exactly {KeyBytes * 2} hex characters ({KeyBytes} bytes). " +
                $"Got {_key.Length} bytes.");
    }

    public string Encrypt(string plaintext)
    {
        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);

        var nonce = new byte[NonceBytes];
        RandomNumberGenerator.Fill(nonce);

        var ciphertext = new byte[plaintextBytes.Length];
        var tag = new byte[TagBytes];

        using var aes = new AesGcm(_key, TagBytes);
        aes.Encrypt(nonce, plaintextBytes, ciphertext, tag);

        return JsonSerializer.Serialize(new
        {
            n = Convert.ToBase64String(nonce),
            c = Convert.ToBase64String(ciphertext),
            t = Convert.ToBase64String(tag)
        });
    }

    public string Decrypt(string cipherJson)
    {
        using var doc = JsonDocument.Parse(cipherJson);
        var root = doc.RootElement;

        var nonce = Convert.FromBase64String(root.GetProperty("n").GetString()!);
        var ciphertext = Convert.FromBase64String(root.GetProperty("c").GetString()!);
        var tag = Convert.FromBase64String(root.GetProperty("t").GetString()!);

        var plaintext = new byte[ciphertext.Length];
        using var aes = new AesGcm(_key, TagBytes);
        aes.Decrypt(nonce, ciphertext, tag, plaintext);

        return Encoding.UTF8.GetString(plaintext);
    }

    public string? DecryptOrNull(string? cipherJson)
    {
        if (string.IsNullOrEmpty(cipherJson)) return null;
        try { return Decrypt(cipherJson); }
        catch { return null; }
    }
}
