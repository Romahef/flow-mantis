using SqlSyncService.Config;
using Microsoft.Extensions.Logging;

namespace SqlSyncService.Admin.Services;

/// <summary>
/// Handles authentication for the admin desktop application.
/// </summary>
public class AdminAuthService
{
    private readonly ConfigStore _configStore;
    private readonly ILogger<AdminAuthService> _logger;

    public AdminAuthService(ConfigStore configStore, ILogger<AdminAuthService> logger)
    {
        _configStore = configStore;
        _logger = logger;
    }

    /// <summary>
    /// Validates admin passphrase.
    /// </summary>
    public bool ValidatePassphrase(string passphrase)
    {
        try
        {
            var settings = _configStore.LoadAppSettings();
            var expectedPassphrase = ConfigStore.Secrets.GetAdminPassphrase(settings);

            if (string.IsNullOrEmpty(expectedPassphrase))
            {
                _logger.LogWarning("Admin passphrase not set in configuration");
                return false;
            }

            // Constant-time comparison
            return CryptographicEquals(passphrase, expectedPassphrase);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating admin passphrase");
            return false;
        }
    }

    private static bool CryptographicEquals(string a, string b)
    {
        if (a.Length != b.Length)
            return false;

        int result = 0;
        for (int i = 0; i < a.Length; i++)
        {
            result |= a[i] ^ b[i];
        }
        return result == 0;
    }
}