# Security Policy

## Supported Versions

| Version | Supported          |
| ------- | ------------------ |
| 1.0.x   | :white_check_mark: |

## Security Features

### Built-in Security

SqlSyncService implements multiple layers of security:

1. **Transport Security**
   - HTTPS/TLS 1.2+ required
   - No HTTP fallback
   - Certificate validation at startup

2. **Authentication & Authorization**
   - API key-based authentication (X-API-Key header)
   - IP allow-list enforcement
   - Constant-time string comparison (prevents timing attacks)

3. **Data Protection**
   - DPAPI encryption for all secrets (LocalMachine scope)
   - Secrets never logged
   - Read-only database access recommended

4. **Request Validation**
   - Input sanitization for query parameters
   - Pagination limits enforced (max 10,000 rows)
   - Timeout controls to prevent resource exhaustion

5. **Audit Trail**
   - All API access logged (IP, endpoint, duration)
   - Authentication failures logged
   - Configuration changes logged

### Security Best Practices

#### Database Access

- **Use SQL Login authentication** (not Windows authentication)
- **Create dedicated read-only user:**
  ```sql
  CREATE LOGIN sqlsync_reader WITH PASSWORD = 'SecurePassword123!';
  CREATE USER sqlsync_reader FOR LOGIN sqlsync_reader;
  GRANT SELECT ON SCHEMA::dbo TO sqlsync_reader;
  ```
- **Restrict to specific tables** if possible
- **Never use sa or admin accounts**

#### Certificate Management

- **Use valid SSL/TLS certificates** (not self-signed in production)
- **Store certificates securely** with proper file permissions
- **Rotate certificates before expiration**
- **Use strong passwords** for PFX files
- **Monitor expiration dates** (check Admin UI)

#### API Key Management

- **Generate cryptographically secure keys** (use built-in generator)
- **Rotate keys regularly** (quarterly recommended)
- **Never commit keys to source control**
- **Store keys in secure password managers**
- **Use different keys per environment** (dev/staging/prod)

#### Network Security

- **Use restrictive IP allow-lists** (specific IPs only)
- **Never use 0.0.0.0/0** or `::/0` in allow-lists
- **Place service behind firewall/WAF** when possible
- **Consider VPN or private network** for sensitive data
- **Monitor for suspicious access patterns**

#### Configuration Security

- **Protect configuration directory:**
  ```powershell
  $acl = Get-Acl "C:\ProgramData\SqlSyncService"
  $acl.SetAccessRuleProtection($true, $false)
  $acl.AddAccessRule((New-Object System.Security.AccessControl.FileSystemAccessRule(
      "SYSTEM", "FullControl", "ContainerInherit,ObjectInherit", "None", "Allow")))
  $acl.AddAccessRule((New-Object System.Security.AccessControl.FileSystemAccessRule(
      "Administrators", "FullControl", "ContainerInherit,ObjectInherit", "None", "Allow")))
  Set-Acl "C:\ProgramData\SqlSyncService" $acl
  ```
- **Backup configurations securely** (encrypted backups)
- **Validate after manual edits** (use Admin UI)

#### Admin UI Security

- **Admin UI is localhost-only** (cannot be accessed remotely by design)
- **Use strong admin passphrase** (16+ characters)
- **Change default passphrase** immediately after install
- **Limit RDP access** to authorized personnel only
- **Enable RDP NLA** (Network Level Authentication)

### Security Checklist

Before deploying to production:

- [ ] Valid SSL/TLS certificate installed
- [ ] Strong API key generated and stored securely
- [ ] IP allow-list contains only authorized IPs
- [ ] Database user has minimal permissions (SELECT only)
- [ ] Admin passphrase changed from default
- [ ] Firewall rules configured correctly
- [ ] Service runs as LocalSystem (not user account)
- [ ] Configuration directory has proper ACLs
- [ ] Logs directory has proper ACLs
- [ ] Certificate file has proper ACLs
- [ ] Integration schema validated
- [ ] Startup validation passes
- [ ] All tests pass
- [ ] Security audit completed

## Reporting a Vulnerability

If you discover a security vulnerability:

1. **DO NOT** create a public GitHub issue
2. **Email** security contact with:
   - Description of the vulnerability
   - Steps to reproduce
   - Potential impact
   - Suggested fix (if any)
3. **Allow** 48 hours for initial response
4. **Coordinate** disclosure timeline

### What to Expect

- **Acknowledgment** within 48 hours
- **Initial assessment** within 1 week
- **Status updates** every 2 weeks
- **Fix timeline** based on severity:
  - Critical: 7 days
  - High: 30 days
  - Medium: 90 days
  - Low: Next release

### Disclosure Policy

- **Responsible disclosure** preferred
- **Coordinated disclosure** after fix available
- **Public acknowledgment** for valid reports (with permission)
- **CVE assignment** for high-severity issues

## Security Updates

Security updates are released as:

- **Hotfixes** for critical vulnerabilities
- **Patches** for high-severity issues
- **Minor versions** for medium/low issues

Subscribe to security advisories through your support channel.

## Compliance

SqlSyncService supports compliance with:

- **GDPR** - Data minimization, right to erasure (via query design)
- **HIPAA** - Encryption at rest (DPAPI) and in transit (HTTPS)
- **SOC 2** - Audit logging, access controls
- **PCI DSS** - (Do not use for cardholder data without additional controls)

**Note:** Compliance requires proper configuration and operational procedures beyond the software itself.

## Security Hardening

### Windows Server Hardening

1. **Disable unnecessary services**
2. **Enable Windows Firewall**
3. **Install security updates** (automatic)
4. **Enable BitLocker** for disk encryption
5. **Configure audit policies**
6. **Disable guest account**
7. **Enforce strong password policies**

### SQL Server Hardening

1. **Disable sa account** or rename
2. **Use strong passwords**
3. **Enable encryption** (TDE for data at rest)
4. **Restrict network access** (private network only)
5. **Enable auditing**
6. **Keep SQL Server updated**
7. **Disable xp_cmdshell** and other dangerous features

### Network Hardening

1. **Use private networks/VLANs**
2. **Enable network segmentation**
3. **Deploy intrusion detection** (IDS/IPS)
4. **Monitor traffic patterns**
5. **Rate limit API requests** (at load balancer)
6. **Use DDoS protection**

## Security Monitoring

Monitor these indicators:

- **Failed authentication attempts** (Event Log)
- **Requests from unexpected IPs** (Event Log)
- **Unusual query patterns** (logs)
- **High CPU/memory usage** (Performance Monitor)
- **Large result sets** (logs)
- **Certificate expiration** (Admin UI)
- **Disk space usage** (logs directory)

Set up alerts for:

- 10+ failed auth attempts in 1 minute
- Requests from non-allowed IPs
- Query timeouts
- Service crashes
- Certificate expiring in < 30 days

## Additional Resources

- [OWASP Top 10](https://owasp.org/www-project-top-ten/)
- [CIS Benchmarks](https://www.cisecurity.org/cis-benchmarks/)
- [Microsoft Security Baseline](https://docs.microsoft.com/en-us/windows/security/threat-protection/windows-security-baselines)
- [SQL Server Security Best Practices](https://docs.microsoft.com/en-us/sql/relational-databases/security/)

---

**Last Updated:** October 5, 2025
