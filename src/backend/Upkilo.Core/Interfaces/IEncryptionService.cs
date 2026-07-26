namespace Upkilo.Core.Interfaces;

public interface IEncryptionService
{
    /// <summary>Encrypt plaintext with AES-256-GCM. Returns a JSON envelope containing nonce, ciphertext, and tag.</summary>
    string Encrypt(string plaintext);

    /// <summary>Decrypt an envelope produced by Encrypt.</summary>
    string Decrypt(string cipherJson);

    /// <summary>Decrypt, returning null if input is null/empty or decryption fails.</summary>
    string? DecryptOrNull(string? cipherJson);
}
