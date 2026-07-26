using System;
using System.Text;

namespace Upkilo.Core.Utilities;

public static class UuidToUlidConverter
{
    private static readonly char[] Base32Chars = "0123456789ABCDEFGHJKMNPQRSTVWXYZ".ToCharArray();

    /// <summary>
    /// Converts a standard Guid (UUID v4) to a ULID-compatible string.
    /// This is used to migrate legacy IDs to the new sortable format.
    /// </summary>
    public static string ToUlid(Guid guid)
    {
        var bytes = guid.ToByteArray();
        
        // ULID is 128 bits, same as GUID.
        // We just need to encode it in Base32.
        var result = new char[26];
        
        // This is a simplified encoding for migration purposes.
        // Real ULID has a specific bit layout (48-bit timestamp, 80-bit randomness).
        // Since we're converting an existing GUID, we lose the timestamp-sortability 
        // for OLD records, but we maintain the data integrity.
        
        for (int i = 0; i < 26; i++)
        {
            // Simplified bit shifting for Base32
            int byteIndex = (i * 5) / 8;
            int bitOffset = (i * 5) % 8;
            
            if (byteIndex < 16)
            {
                int val = bytes[byteIndex] >> bitOffset;
                if (bitOffset > 3 && byteIndex + 1 < 16)
                {
                    val |= bytes[byteIndex + 1] << (8 - bitOffset);
                }
                result[i] = Base32Chars[val & 0x1F];
            }
            else
            {
                result[i] = '0';
            }
        }
        
        return new string(result);
    }
}
