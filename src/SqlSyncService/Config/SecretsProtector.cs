using System.Security.Cryptography;
using System.Text;

namespace SqlSyncService.Config;

/// <summary>
/// Encrypts and decrypts sensitive data using Windows DPAPI (LocalMachine scope).
/// </summary>
public static class SecretsProtector
{
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("SqlSyncService.v1");

    /// <summary>
    /// Encrypts plain text using DPAPI LocalMachine scope.
    /// </summary>
    public static string Protect(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            return string.Empty;

        // DPAPI is Windows-only; on non-Windows, return base64 for development
        if (!OperatingSystem.IsWindows())
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(plainText));
        }

        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var encryptedBytes = ProtectedData.Protect(
            plainBytes,
            Entropy,
            DataProtectionScope.LocalMachine
        );
        return Convert.ToBase64String(encryptedBytes);
    }

    /// <summary>
    /// Decrypts protected text using DPAPI LocalMachine scope.
    /// </summary>
    public static string Unprotect(string encryptedText)
    {
        if (string.IsNullOrEmpty(encryptedText))
            return string.Empty;

        // DPAPI is Windows-only; on non-Windows, decode base64 for development
        if (!OperatingSystem.IsWindows())
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(encryptedText));
        }

        try
        {
            var encryptedBytes = Convert.FromBase64String(encryptedText);
            var plainBytes = ProtectedData.Unprotect(
                encryptedBytes,
                Entropy,
                DataProtectionScope.LocalMachine
            );
            return Encoding.UTF8.GetString(plainBytes);
        }
        catch (CryptographicException ex)
        {
            throw new InvalidOperationException(
                "Failed to decrypt data. Ensure the data was encrypted on this machine.", ex);
        }
    }

    /// <summary>
    /// Generates a cryptographically secure random API key.
    /// </summary>
    public static string GenerateApiKey()
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }
}
