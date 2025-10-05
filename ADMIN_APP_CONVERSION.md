# Admin Interface Conversion: Web → Desktop App

## Overview

The Admin interface has been **converted from a web application to a native Windows desktop application** based on user feedback.

---

## ✅ What Changed

### Before (Web Interface)
- **Technology:** ASP.NET Core Razor Pages
- **Access:** Browser at `https://localhost:9443/admin`
- **Architecture:** Web server with HTTP middleware
- **Dependencies:** Browser required

### After (Desktop Application)
- **Technology:** WPF (Windows Presentation Foundation)
- **Access:** Native Windows executable `SqlSyncService.Admin.exe`
- **Architecture:** Standalone desktop app
- **Dependencies:** .NET 8 Desktop Runtime

---

## 🎨 New Desktop Application

### Features

**Windows & Dialogs:**
- ✅ **LoginWindow** - Secure passphrase authentication
- ✅ **MainWindow** - Tab-based main interface
  - Security Tab (API keys, IP allow-list, certificates)
  - Database Tab (connection settings, testing)
  - Queries Tab (DataGrid with add/edit/delete)
  - Mapping Tab (view endpoint mappings)
  - About Tab (version info, features)
- ✅ **QueryEditorWindow** - Full-featured query editor dialog
  - SQL text editor (monospace font)
  - Pagination settings
  - Offset/Token mode selector
  - Validation

**UI/UX:**
- ✅ Modern, clean design with consistent styling
- ✅ Color-coded alerts (success/error/warning)
- ✅ Responsive layout
- ✅ Keyboard shortcuts (Enter to submit forms)
- ✅ Tab navigation
- ✅ Visual feedback for all actions

**Functionality:**
- ✅ All previous features preserved
- ✅ Direct file I/O (no HTTP overhead)
- ✅ Faster performance
- ✅ Better Windows integration
- ✅ Desktop shortcut support

---

## 📁 File Structure Changes

### Removed Files
```
❌ src/SqlSyncService.Admin/Program.cs (web host)
❌ src/SqlSyncService.Admin/Pages/Admin.cshtml
❌ src/SqlSyncService.Admin/Pages/Admin.cshtml.cs
❌ Package dependencies: ASP.NET Core, Razor, Antiforgery
```

### Added Files
```
✅ src/SqlSyncService.Admin/App.xaml (WPF app definition)
✅ src/SqlSyncService.Admin/App.xaml.cs (app startup logic)
✅ src/SqlSyncService.Admin/MainWindow.xaml (main UI)
✅ src/SqlSyncService.Admin/MainWindow.xaml.cs (main logic)
✅ src/SqlSyncService.Admin/LoginWindow.xaml (login dialog)
✅ src/SqlSyncService.Admin/LoginWindow.xaml.cs (login logic)
✅ src/SqlSyncService.Admin/QueryEditorWindow.xaml (query editor)
✅ src/SqlSyncService.Admin/QueryEditorWindow.xaml.cs (editor logic)
```

### Modified Files
```
📝 src/SqlSyncService.Admin/SqlSyncService.Admin.csproj
   - Changed from <Sdk="Microsoft.NET.Sdk.Web"> to <Sdk="Microsoft.NET.Sdk">
   - Changed OutputType to WinExe
   - Changed TargetFramework to net8.0-windows
   - Added UseWPF property
   - Removed web packages

📝 src/SqlSyncService.Admin/Services/AdminAuthService.cs
   - Removed HttpContext dependency
   - Simplified for desktop use

📝 src/SqlSyncService.Admin/Services/AdminApplyService.cs
   - No changes needed (already decoupled)
```

---

## 🚀 How to Use

### Building

```powershell
# From solution directory
dotnet build src/SqlSyncService.Admin/SqlSyncService.Admin.csproj -c Release
```

**Output:** `src/SqlSyncService.Admin/bin/Release/net8.0-windows/SqlSyncService.Admin.exe`

### Running

**Method 1: Direct execution**
```powershell
.\SqlSyncService.Admin.exe
```

**Method 2: From Visual Studio**
- Set `SqlSyncService.Admin` as startup project
- Press F5

**Method 3: Desktop shortcut** (after installation)
- Double-click "SqlSyncService Admin" shortcut

### Development

```powershell
# Set config directory for testing
$env:SQLSYNC_CONFIG_DIR = "C:\Dev\SqlSyncTest"

# Run in development mode
dotnet run --project src/SqlSyncService.Admin/SqlSyncService.Admin.csproj
```

---

## 🔧 Technical Details

### Architecture

```
App.xaml.cs (Entry Point)
    ↓
Dependency Injection Setup
    ↓
LoginWindow (Authentication)
    ↓ (if authenticated)
MainWindow (Main Interface)
    ├─ Security Tab
    ├─ Database Tab
    ├─ Queries Tab → QueryEditorWindow
    ├─ Mapping Tab
    └─ About Tab
    ↓
Services Layer
    ├─ AdminAuthService
    └─ AdminApplyService
    ↓
ConfigStore (File I/O)
```

### Dependency Injection

The desktop app uses Microsoft.Extensions.DependencyInjection:

```csharp
services.AddSingleton<ConfigStore>();
services.AddSingleton<AdminAuthService>();
services.AddSingleton<AdminApplyService>();
services.AddTransient<MainWindow>();
services.AddTransient<LoginWindow>();
```

### Data Binding

WPF data binding used for:
- IP allow-list (ListBox)
- Queries grid (DataGrid)
- Form fields (Two-way binding)

---

## 🎨 UI Design

### Color Scheme

- **Primary:** #2563eb (Blue)
- **Success:** #059669 (Green)
- **Danger:** #dc2626 (Red)
- **Warning:** #f59e0b (Amber)
- **Background:** #f5f5f5 (Light Gray)
- **Surface:** #ffffff (White)

### Typography

- **Headers:** 18-24pt, Bold
- **Body:** 14pt, Regular
- **Labels:** 14pt, SemiBold
- **Code:** Consolas 13pt (SQL editor)

### Layout

- **Margins:** 20-24px consistent spacing
- **Padding:** 12-20px for cards/panels
- **Border Radius:** 6-8px for modern look
- **Shadows:** Subtle drop shadows on elevated surfaces

---

## ✅ Advantages of Desktop App

### Performance
- ✅ No HTTP overhead
- ✅ Direct file access
- ✅ Lower memory usage
- ✅ Faster startup

### User Experience
- ✅ Native Windows controls
- ✅ Familiar desktop patterns
- ✅ Keyboard navigation
- ✅ System tray integration potential
- ✅ Better error handling

### Security
- ✅ No network listener (attack surface reduced)
- ✅ No HTTPS certificate needed for admin
- ✅ No CSRF tokens required
- ✅ Direct OS-level security

### Deployment
- ✅ Single EXE file
- ✅ Easy desktop shortcuts
- ✅ Can run without service running
- ✅ Simpler troubleshooting

---

## 📦 Distribution

### Standalone Executable

The admin app can be distributed as:

1. **Part of installer** - Installed alongside service
2. **Standalone tool** - Separate download for admins
3. **Portable** - No installation required (with .NET runtime)

### Requirements

- **OS:** Windows 10 1809+ or Windows Server 2019+
- **Runtime:** .NET 8 Desktop Runtime
- **Permissions:** Write access to `C:\ProgramData\SqlSyncService\`

---

## 🔄 Migration Notes

### For Users

**No action required!** The functionality is identical:

- ✅ Same authentication (passphrase)
- ✅ Same features and tabs
- ✅ Same configuration files
- ✅ Same security model

**What's different:**
- 🔄 Launch exe instead of browser
- 🔄 Looks like a Windows app, not a website
- ✅ Faster and more responsive

### For Developers

**Project changes:**
- Update .csproj to use WPF SDK
- Replace Razor Pages with XAML
- Remove web middleware
- Update documentation

**Testing:**
- Desktop UI testing (manual or automated with FlaUI/Appium)
- No browser-based testing needed
- Focus on Windows-specific features

---

## 🐛 Known Issues / Limitations

### Current Limitations

1. **Windows Only** - Cannot run on macOS/Linux (by design)
2. **Requires Desktop Runtime** - Not just ASP.NET runtime
3. **No Remote Access** - Must have local/RDP access (security feature)

### Potential Enhancements

- [ ] Add command-line arguments for automation
- [ ] System tray icon for quick access
- [ ] Real-time service status monitoring
- [ ] Log viewer integrated into app
- [ ] Dark mode theme
- [ ] Export/import configuration

---

## 📝 Documentation Updated

All documentation has been updated to reflect the desktop app:

- ✅ README.md - Updated admin section
- ✅ PROJECT_SUMMARY.md - Architecture diagrams
- ✅ BUILD_INSTRUCTIONS.md - Desktop build steps
- ✅ QUICK_START_MACOS.md - Testing instructions
- ✅ CHANGELOG.md - Version notes

---

## 🎯 Summary

**The admin interface is now a proper Windows desktop application** that integrates seamlessly with the Windows environment while maintaining all the functionality of the web interface. This provides a better user experience, improved performance, and reduced attack surface.

**Migration is transparent to end users** - the same features in a better package!

---

**Last Updated:** October 5, 2025  
**Version:** 1.0.0
