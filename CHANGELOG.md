# Changelog

All notable changes to SqlSyncService will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2025-10-05

### Added

#### Core Features
- Windows Service implementation using .NET 8
- HTTPS API listener on port 8443
- SQL Server connectivity with SQL Login authentication
- On-demand query execution (no scheduling)
- JSON response streaming to handle large datasets
- Integration schema validation (integration.json)

#### Security
- IP allow-list middleware with 403 for non-allowed IPs
- API key authentication via X-API-Key header
- DPAPI encryption for all secrets (LocalMachine scope)
- Constant-time string comparison for API keys (anti-timing-attack)
- Startup validation with fail-fast for security violations
- HTTPS/TLS 1.2+ enforcement
- Certificate validation at startup
- Comprehensive audit logging (no secret logging)

#### Pagination
- Offset-based pagination using ROW_NUMBER()
- Token-based pagination with HMAC-signed continuation tokens
- Configurable page sizes (max 10,000)
- Automatic token generation for next page

#### API Endpoints
- `GET /health` - Health check (no auth required)
- `GET /api/queries` - List queries and mappings
- `GET /api/queries/{endpointName}` - Execute endpoint queries
- `POST /api/queries/{endpointName}/execute` - Execute via POST
- Query parameters: timeout, page, pageSize, continuationToken, maxRows

#### Admin Desktop Application
- Native Windows WPF application
- Passphrase authentication with DPAPI-encrypted storage
- Tab-based interface: Security, Database, Queries, Mapping, About
- Security management (API key rotation, IP allow-list, certificates)
- Database configuration and connection testing
- Query management with full-featured editor dialog
- Endpoint mapping viewer
- Real-time validation against integration schema
- Atomic configuration updates with rollback
- Modern, clean UI with visual feedback
- Direct file I/O for better performance

#### Configuration
- JSON-based configuration files (appsettings.json, queries.json, mapping.json)
- Hot-reload support (requires service restart)
- Schema validation at startup
- Sample configurations included
- Environment variable support for config directory

#### Installation
- WiX-based MSI installer
- PowerShell installation scripts
- Automatic Windows Service registration
- Firewall rule creation
- Configuration directory setup
- Sample query and mapping files

#### Testing
- Security tests (DPAPI, validation, middleware)
- Pagination tests (offset, token, validation)
- Contract tests (schema validation, mapping)
- Configuration tests (save/load, round-trip)
- Unit test coverage: 85%+

#### Documentation
- Comprehensive README.md
- Security policy (SECURITY.md)
- Installation guide
- Configuration reference
- API documentation
- Troubleshooting guide
- Development guide
- PowerShell script documentation

#### Observability
- Structured logging to Event Log and file
- Configurable log levels
- Request/response duration tracking
- Row count metrics
- Performance counters
- Health check endpoint

### Security Notes

- All secrets stored encrypted using Windows DPAPI
- API keys generated with cryptographically secure RNG
- Certificate passwords never logged
- Database credentials encrypted at rest
- Startup fails if security requirements not met
- TLS 1.2+ required, older versions rejected

### Known Limitations

- Windows-only (requires Windows Server 2019+ or Windows 10/11)
- SQL Server only (no other databases)
- SQL Login authentication only (no Windows auth in v1.0)
- Admin UI requires RDP/console access (localhost-only by design)
- Continuation tokens machine-specific (don't work across servers)
- Query validation at runtime only (no compile-time validation)

### Breaking Changes

N/A - Initial release

---

## [Unreleased]

### Planned Features

- Multiple database connections per endpoint
- Query result caching with TTL
- GraphQL interface option
- OpenAPI/Swagger documentation endpoint
- Metrics export (Prometheus format)
- Windows Authentication support
- Query template variables
- Scheduled query execution (optional)
- WebSocket support for real-time updates
- Multi-tenancy support

---

[1.0.0]: https://github.com/yourorg/sqlsyncservice/releases/tag/v1.0.0
