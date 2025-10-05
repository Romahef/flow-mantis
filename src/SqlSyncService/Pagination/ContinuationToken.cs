using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SqlSyncService.Pagination;

/// <summary>
/// Creates and validates HMAC-signed continuation tokens for secure pagination.
/// </summary>
public class ContinuationToken
{
    private static readonly byte[] SecretKey = DeriveKey();

    private static byte[] DeriveKey()
    {
        // In production, this should be stored in configuration
        // For now, derive from machine-specific data
        var machineKey = Environment.MachineName + Environment.UserName;
        return SHA256.HashData(Encoding.UTF8.GetBytes(machineKey));
    }

    /// <summary>
    /// Creates a signed continuation token from key column values.
    /// </summary>
    public static string Create(Dictionary<string, object?> lastKeyValues)
    {
        var json = JsonSerializer.Serialize(lastKeyValues);
        var jsonBytes = Encoding.UTF8.GetBytes(json);
        
        // Create HMAC signature
        using var hmac = new HMACSHA256(SecretKey);
        var signature = hmac.ComputeHash(jsonBytes);
        
        // Combine data + signature
        var combined = new byte[jsonBytes.Length + signature.Length];
        Buffer.BlockCopy(jsonBytes, 0, combined, 0, jsonBytes.Length);
        Buffer.BlockCopy(signature, 0, combined, jsonBytes.Length, signature.Length);
        
        return Convert.ToBase64String(combined);
    }

    /// <summary>
    /// Validates and extracts key values from a continuation token.
    /// Returns null if token is invalid or tampered.
    /// </summary>
    public static Dictionary<string, object?>? Validate(string token)
    {
        try
        {
            var combined = Convert.FromBase64String(token);
            
            if (combined.Length < 32) // Minimum: empty JSON + 32-byte signature
                return null;
            
            // Split data and signature
            var signatureLength = 32; // SHA256 = 32 bytes
            var jsonBytes = new byte[combined.Length - signatureLength];
            var signature = new byte[signatureLength];
            
            Buffer.BlockCopy(combined, 0, jsonBytes, 0, jsonBytes.Length);
            Buffer.BlockCopy(combined, jsonBytes.Length, signature, 0, signatureLength);
            
            // Verify signature
            using var hmac = new HMACSHA256(SecretKey);
            var expectedSignature = hmac.ComputeHash(jsonBytes);
            
            if (!signature.SequenceEqual(expectedSignature))
                return null;
            
            // Deserialize data
            var json = Encoding.UTF8.GetString(jsonBytes);
            return JsonSerializer.Deserialize<Dictionary<string, object?>>(json);
        }
        catch
        {
            return null;
        }
    }
}
