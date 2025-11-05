# Port Configuration Feature

## Overview
Added dynamic API port configuration to both the installer and Admin UI, allowing users to:
- Select a custom port during installation
- Change the port later through the Admin UI
- Configure port forwarding for external access

## Changes Made

### 1. Installer (`src/SqlSyncService.InstallerWizard/`)

#### UI Changes (`MainWindow.xaml`)
- **Added API Port Input Field** (Security Configuration Page):
  - Text field for entering custom port (default: 8080 for HTTP, 8443 for HTTPS)
  - Port validation (1024-65535)
  - Informational text about port forwarding requirements
  
- **Updated Security Mode Radio Buttons**:
  - Removed hard-coded port numbers from labels
  - Radio buttons now automatically suggest appropriate default ports:
    - HTTP → 8080
    - HTTPS (any type) → 8443
  - Port auto-suggestion only happens for default ports

#### Code Changes (`MainWindow.xaml.cs`)
- **Enhanced `SecurityMode_Changed()` Event**:
  - Automatically suggests appropriate port based on HTTP/HTTPS selection
  - Only auto-updates if user hasn't manually changed from default ports

- **Added Port Validation in `ValidateSecurity()`**:
  - Ensures port is between 1024-65535
  - Shows helpful error message with common port suggestions

- **Updated `CreateConfigurationFilesAsync()`**:
  - Uses user-selected port instead of hard-coded values
  - Dynamically constructs `Service.ListenUrl` with custom port

- **Enhanced `ConfigureFirewall()`**:
  - Creates firewall rule for custom port instead of fixed 8443
  - Removes old rules and creates new "SqlSyncService API" rule
  - Logs which port was configured

### 2. Admin UI (`src/SqlSyncService.Admin/`)

#### UI Changes (`MainWindow.xaml`)
- **Added API Port Section** (Security Tab):
  - Displays current API port
  - Allows editing the port
  - Clear instructions about service restart requirement
  - "Update Port" button to save changes

#### Code Changes (`MainWindow.xaml.cs`)
- **Enhanced `PopulateSecurityTab()`**:
  - Extracts port from `Service.ListenUrl` configuration
  - Displays current port in `TxtApiPortAdmin` field
  - Falls back to default port 8080 if extraction fails

- **New `UpdateApiPort_Click()` Handler**:
  - Validates port range (1024-65535)
  - Preserves HTTP/HTTPS protocol from current configuration
  - Updates `Service.ListenUrl` with new port while keeping protocol
  - Saves configuration to `appsettings.json`
  - Shows success message reminding user to restart service

#### Bug Fixes (`Services/AdminAuthService.cs`)
- **Removed Debug Code**:
  - Cleaned up all debug logging statements
  - Removed debug message boxes
  - Streamlined password validation logic
  - Kept only essential error logging

## User Experience

### During Installation:
1. User enters database connection details
2. On Security Configuration page:
   - User selects security mode (HTTP, HTTPS Self-Signed, Let's Encrypt, or Custom Cert)
   - **Port field auto-suggests appropriate default** (8080 or 8443)
   - User can customize port to any value between 1024-65535
   - Port is validated before proceeding
3. Installer:
   - Configures service with selected port
   - Creates Windows Firewall rule for that port
   - Saves configuration to `appsettings.json`

### After Installation:
1. User opens Admin UI
2. Goes to **Security Tab**
3. Sees **API Listen Port** section showing current port
4. Can enter new port (1024-65535)
5. Clicks **"💾 Update Port"**
6. Receives confirmation message
7. **Restarts SqlSyncService** for changes to take effect

## Configuration File Structure

The port is stored in `appsettings.json` as part of `Service.ListenUrl`:

```json
{
  "Service": {
    "ListenUrl": "https://0.0.0.0:8443"
  },
  ...
}
```

- **Protocol**: `http://` or `https://` (based on security mode)
- **Host**: `0.0.0.0` (listens on all network interfaces)
- **Port**: User-defined custom port

## Technical Details

### Port Selection Logic
1. **Default Ports**:
   - HTTP: 8080
   - HTTPS: 8443
   
2. **Auto-Suggestion**:
   - When switching between HTTP/HTTPS modes, port auto-updates only if current port is 8080 or 8443
   - User's custom port selections are preserved when switching modes

3. **Validation**:
   - Minimum: 1024 (ports below are system reserved)
   - Maximum: 65535 (highest valid port)
   - Must be integer

### Firewall Configuration
- **Rule Name**: "SqlSyncService API"
- **Direction**: Inbound
- **Action**: Allow
- **Protocol**: TCP
- **Port**: User-defined custom port
- Automatically removes old rules during installation

### Service Restart
Changing the port requires restarting the SqlSyncService Windows Service:
```powershell
Restart-Service SqlSyncService
```

## Benefits

1. **Flexibility**: Users can choose any available port for their environment
2. **Network Compliance**: Allows adherence to organization-specific port policies
3. **Port Forwarding**: Makes it easy to configure router/firewall port forwarding
4. **Multiple Instances**: Enables running multiple instances on different ports (advanced scenario)
5. **Conflict Avoidance**: Users can avoid ports already in use by other services

## Notes

- **Service Restart Required**: Port changes only take effect after restarting the Windows Service
- **Firewall**: Windows Firewall rule is automatically updated during installation
- **Router/Firewall**: Users must manually configure external port forwarding if accessing from outside local network
- **HTTPS Certificates**: When using HTTPS, ensure certificate is bound to the new port
- **Default Recommendation**: 8080 for HTTP, 8443 for HTTPS

## Files Modified

### Installer:
- `src/SqlSyncService.InstallerWizard/MainWindow.xaml`
- `src/SqlSyncService.InstallerWizard/MainWindow.xaml.cs`

### Admin UI:
- `src/SqlSyncService.Admin/MainWindow.xaml`
- `src/SqlSyncService.Admin/MainWindow.xaml.cs`
- `src/SqlSyncService.Admin/Services/AdminAuthService.cs` (cleanup only)

## Future Enhancements (Optional)

1. **Port Availability Check**: Verify port is not in use before applying
2. **Service Auto-Restart**: Automatically restart service after port change
3. **Multiple Listen URLs**: Support binding to multiple ports simultaneously
4. **Port Range Selection**: Allow specifying a range for load balancing
5. **IPv4/IPv6 Selection**: Allow choosing specific network interfaces

---

**Version**: 1.0  
**Date**: October 2025  
**Status**: Implemented and Tested

