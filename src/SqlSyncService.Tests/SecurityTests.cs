using Xunit;
using SqlSyncService.Config;
using SqlSyncService.Security;
using Microsoft.Extensions.Logging.Abstractions;

namespace SqlSyncService.Tests;

public class SecurityTests
{
    [Fact]
    public void SecretsProtector_ProtectAndUnprotect_RoundTrip()
    {
        // Arrange
        var plainText = "MySecretPassword123!";

        // Act
        var encrypted = SecretsProtector.Protect(plainText);
        var decrypted = SecretsProtector.Unprotect(encrypted);

        // Assert
        Assert.NotEqual(plainText, encrypted);
        Assert.Equal(plainText, decrypted);
    }

    [Fact]
    public void SecretsProtector_GenerateApiKey_ReturnsValidKey()
    {
        // Act
        var apiKey = SecretsProtector.GenerateApiKey();

        // Assert
        Assert.NotNull(apiKey);
        Assert.True(apiKey.Length > 30);
        
        // Should be valid base64
        var bytes = Convert.FromBase64String(apiKey);
        Assert.Equal(32, bytes.Length);
    }

    [Fact]
    public void StartupValidator_ValidSettings_PassesValidation()
    {
        // Arrange
        var settings = new AppSettings
        {
            Service = new ServiceConfig { ListenUrl = "https://localhost:8443" },
            Security = new SecurityConfig
            {
                EnableHttps = true,
                RequireApiKey = true,
                ApiKeyEncrypted = SecretsProtector.Protect("test-api-key"),
                Certificate = new CertificateConfig
                {
                    Path = CreateTestCertificate(),
                    PasswordEncrypted = SecretsProtector.Protect("")
                }
            },
            Database = new DatabaseConfig
            {
                Server = "localhost",
                Database = "TestDb",
                UsernameEncrypted = SecretsProtector.Protect("testuser"),
                PasswordEncrypted = SecretsProtector.Protect("testpass")
            }
        };

        var logger = NullLogger.Instance;

        // Act & Assert - Should not throw
        var exception = Record.Exception(() => 
            StartupValidator.ValidateSecurityRequirements(settings, logger));
        
        Assert.Null(exception);
    }

    [Fact]
    public void StartupValidator_NonLoopbackWithoutIpAllowList_ThrowsException()
    {
        // Arrange
        var settings = new AppSettings
        {
            Service = new ServiceConfig { ListenUrl = "https://0.0.0.0:8443" },
            Security = new SecurityConfig
            {
                EnableHttps = true,
                IpAllowList = new List<string>(), // Empty!
                RequireApiKey = true,
                ApiKeyEncrypted = SecretsProtector.Protect("test-key"),
                Certificate = new CertificateConfig
                {
                    Path = CreateTestCertificate(),
                    PasswordEncrypted = SecretsProtector.Protect("")
                }
            },
            Database = new DatabaseConfig
            {
                Server = "localhost",
                Database = "TestDb",
                UsernameEncrypted = SecretsProtector.Protect("user"),
                PasswordEncrypted = SecretsProtector.Protect("pass")
            }
        };

        var logger = NullLogger.Instance;

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            StartupValidator.ValidateSecurityRequirements(settings, logger));
        
        Assert.Contains("allow-list", exception.Message.ToLower());
    }

    [Fact]
    public void StartupValidator_MissingCertificate_ThrowsException()
    {
        // Arrange
        var settings = new AppSettings
        {
            Service = new ServiceConfig { ListenUrl = "https://localhost:8443" },
            Security = new SecurityConfig
            {
                EnableHttps = true,
                RequireApiKey = true,
                ApiKeyEncrypted = SecretsProtector.Protect("test-key"),
                Certificate = new CertificateConfig
                {
                    Path = "C:\\nonexistent\\cert.pfx",
                    PasswordEncrypted = SecretsProtector.Protect("")
                }
            },
            Database = new DatabaseConfig
            {
                Server = "localhost",
                Database = "TestDb",
                UsernameEncrypted = SecretsProtector.Protect("user"),
                PasswordEncrypted = SecretsProtector.Protect("pass")
            }
        };

        var logger = NullLogger.Instance;

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            StartupValidator.ValidateSecurityRequirements(settings, logger));
        
        Assert.Contains("certificate", exception.Message.ToLower());
    }

    private string CreateTestCertificate()
    {
        // Create a temporary self-signed certificate for testing
        var certPath = Path.Combine(Path.GetTempPath(), $"test-cert-{Guid.NewGuid()}.pfx");
        
        var sanBuilder = new System.Security.Cryptography.X509Certificates.SubjectAlternativeNameBuilder();
        sanBuilder.AddDnsName("localhost");

        var distinguishedName = new System.Security.Cryptography.X509Certificates.X500DistinguishedName("CN=Test");

        using var rsa = System.Security.Cryptography.RSA.Create(2048);
        var request = new System.Security.Cryptography.X509Certificates.CertificateRequest(
            distinguishedName, rsa,
            System.Security.Cryptography.HashAlgorithmName.SHA256,
            System.Security.Cryptography.RSASignaturePadding.Pkcs1);

        request.CertificateExtensions.Add(sanBuilder.Build());

        var certificate = request.CreateSelfSigned(
            DateTimeOffset.Now.AddDays(-1),
            DateTimeOffset.Now.AddYears(1));

        File.WriteAllBytes(certPath, certificate.Export(System.Security.Cryptography.X509Certificates.X509ContentType.Pfx));
        
        return certPath;
    }
}
