using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Data.SqlClient;

namespace SqlSyncService.InstallerWizard
{
public partial class MainWindow : Window
{
        private int currentPage = 0;
        private readonly List<FrameworkElement> pages;
        private string? generatedApiKey;
        private const string InstallPath = @"C:\Program Files\SqlSyncService";
        private const string ConfigPath = @"C:\ProgramData\SqlSyncService";

    public MainWindow()
    {
        InitializeComponent();
            
            pages = new List<FrameworkElement>
            {
                WelcomePage,
                DatabasePage,
                SecurityPage,
                InstallingPage,
                CompletePage
            };
            
            ShowPage(0);
        }

        private void ShowPage(int pageIndex)
        {
            // Hide all pages
            foreach (var page in pages)
            {
                page.Visibility = Visibility.Collapsed;
            }
            
            // Show current page
            pages[pageIndex].Visibility = Visibility.Visible;
            currentPage = pageIndex;
            
            // Update header
            PageTitle.Text = pageIndex switch
            {
                0 => "Welcome",
                1 => "Database Configuration",
                2 => "Security Configuration",
                3 => "Installing...",
                4 => "Installation Complete",
                _ => ""
            };
            
            // Update navigation buttons
            BtnBack.IsEnabled = pageIndex > 0 && pageIndex < 3;
            BtnNext.IsEnabled = pageIndex < 3;
            
            if (pageIndex == 3)
            {
                BtnCancel.IsEnabled = false;
                BtnNext.Content = "Finish";
            }
            else if (pageIndex == 4)
            {
                BtnCancel.IsEnabled = false;
                BtnBack.IsEnabled = false;
                BtnNext.Content = "Finish";
                BtnNext.IsEnabled = true;
            }
            else if (pageIndex == 2)
            {
                BtnNext.Content = "Install";
            }
            else
            {
                BtnNext.Content = "Next →";
            }
        }

        private void Next_Click(object sender, RoutedEventArgs e)
        {
            if (currentPage == 4)
            {
                // Finish button on complete page
                if (ChkStartService.IsChecked == true)
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = "sc",
                            Arguments = "start SqlSyncService",
                            UseShellExecute = false,
                            CreateNoWindow = true
                        })?.WaitForExit();
                    }
                    catch { }
                }
                Application.Current.Shutdown();
                return;
            }
            
            // Validate before moving forward
            if (currentPage == 1 && !ValidateDatabase())
                return;
            
            if (currentPage == 2 && !ValidateSecurity())
                return;
            
            if (currentPage == 2)
            {
                // Start installation
                ShowPage(3);
                _ = PerformInstallationAsync();
            }
            else
            {
                ShowPage(currentPage + 1);
            }
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            if (currentPage > 0)
                ShowPage(currentPage - 1);
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to cancel the installation?", 
                "Cancel Installation", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                Application.Current.Shutdown();
            }
        }

        private bool ValidateDatabase()
        {
            if (string.IsNullOrWhiteSpace(TxtDbName.Text))
            {
                MessageBox.Show("Please enter a database name.", "Validation Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            
            if (string.IsNullOrWhiteSpace(TxtDbUser.Text))
            {
                MessageBox.Show("Please enter a SQL username.", "Validation Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            
            if (string.IsNullOrWhiteSpace(TxtDbPassword.Password))
            {
                MessageBox.Show("Please enter a SQL password.", "Validation Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            
            return true;
        }

        private bool ValidateSecurity()
        {
            if (string.IsNullOrWhiteSpace(TxtAdminPassphrase.Password))
            {
                MessageBox.Show("Please enter an admin passphrase.", "Validation Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            
            return true;
        }

        private async void TestConnection_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateDatabase())
                return;
            
            TxtConnectionStatus.Text = "Testing connection...";
            TxtConnectionStatus.Foreground = System.Windows.Media.Brushes.Orange;
            
            try
            {
                var builder = new SqlConnectionStringBuilder
                {
                    DataSource = TxtDbPort.Text == "1433" 
                        ? TxtDbServer.Text 
                        : $"{TxtDbServer.Text},{TxtDbPort.Text}",
                    InitialCatalog = TxtDbName.Text,
                    UserID = TxtDbUser.Text,
                    Password = TxtDbPassword.Password,
                    TrustServerCertificate = ChkTrustCertificate.IsChecked == true,
                    Encrypt = true,
                    ConnectTimeout = 10
                };
                
                using var connection = new SqlConnection(builder.ConnectionString);
                await connection.OpenAsync();
                
                TxtConnectionStatus.Text = "✓ Connection successful!";
                TxtConnectionStatus.Foreground = System.Windows.Media.Brushes.Green;
            }
            catch (Exception ex)
            {
                TxtConnectionStatus.Text = $"✗ Connection failed: {ex.Message}";
                TxtConnectionStatus.Foreground = System.Windows.Media.Brushes.Red;
            }
        }

        private void BrowseCert_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Certificate Files (*.pfx)|*.pfx|All Files (*.*)|*.*",
                Title = "Select SSL Certificate"
            };
            
            if (dialog.ShowDialog() == true)
            {
                TxtCertPath.Text = dialog.FileName;
            }
        }

        private async Task PerformInstallationAsync()
        {
            try
            {
                LogInstall("Starting installation...");
                
                // Step 1: Create directories
                LogInstall("[1/8] Creating directories...");
                Directory.CreateDirectory(InstallPath);
                Directory.CreateDirectory(ConfigPath);
                Directory.CreateDirectory(Path.Combine(ConfigPath, "certs"));
                Directory.CreateDirectory(Path.Combine(ConfigPath, "logs"));
                
                // Step 2: Extract service files
                LogInstall("[2/8] Extracting service files...");
                ExtractServiceFiles();
                
                // Step 3: Extract Admin UI
                LogInstall("[3/8] Extracting Admin UI...");
                ExtractAdminUI();
                
                // Step 4: Generate API key if needed
                LogInstall("[4/8] Generating API key...");
                generatedApiKey = string.IsNullOrWhiteSpace(TxtApiKey.Text) 
                    ? GenerateApiKey() 
                    : TxtApiKey.Text;
                
                // Step 5: Create configuration files
                LogInstall("[5/8] Creating configuration files...");
                await CreateConfigurationFilesAsync();
                
                // Step 6: Install Windows Service
                LogInstall("[6/8] Installing Windows Service...");
                InstallWindowsService();
                
                // Step 7: Configure firewall
                LogInstall("[7/8] Configuring Windows Firewall...");
                ConfigureFirewall();
                
                // Step 8: Create shortcuts
                LogInstall("[8/8] Creating desktop shortcuts...");
                CreateShortcuts();
                
                LogInstall("");
                LogInstall("Installation completed successfully!");
                
                // Show completion page
                TxtApiKeyDisplay.Text = $"API Key (SAVE THIS SECURELY):\n{generatedApiKey}";
                TxtServiceStatus.Text = "Service installed and ready to start";
                TxtServiceStatus.Foreground = System.Windows.Media.Brushes.Green;
                
                ShowPage(4);
            }
            catch (Exception ex)
            {
                LogInstall("");
                LogInstall($"ERROR: Installation failed!");
                LogInstall(ex.ToString());
                
                MessageBox.Show($"Installation failed:\n\n{ex.Message}\n\nCheck the log for details.", 
                    "Installation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                
                BtnCancel.IsEnabled = true;
            }
        }

        private void ExtractServiceFiles()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "service.zip";
            
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                throw new Exception("Embedded service files not found. Please rebuild the installer.");
            }
            
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
            archive.ExtractToDirectory(InstallPath, true);
            
            LogInstall($"Extracted {archive.Entries.Count} service files to {InstallPath}");
        }

        private void ExtractAdminUI()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "admin.zip";
            
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                LogInstall("Warning: Admin UI files not found in installer. Skipping...");
                return;
            }
            
            var adminPath = Path.Combine(InstallPath, "Admin");
            Directory.CreateDirectory(adminPath);
            
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
            archive.ExtractToDirectory(adminPath, true);
            
            LogInstall($"Extracted {archive.Entries.Count} Admin UI files to {adminPath}");
        }

        private async Task CreateConfigurationFilesAsync()
        {
            var assembly = Assembly.GetExecutingAssembly();
            
            // Load templates
            var appSettingsTemplate = await LoadEmbeddedResourceAsync("appsettings.json");
            var queriesTemplate = await LoadEmbeddedResourceAsync("queries.json");
            var mappingTemplate = await LoadEmbeddedResourceAsync("mapping.json");
            var integrationTemplate = await LoadEmbeddedResourceAsync("integration.json");
            
            // Create appsettings.json with user configuration
            var appSettings = JsonDocument.Parse(appSettingsTemplate);
            var root = appSettings.RootElement;
            
            using var ms = new MemoryStream();
            using var writer = new Utf8JsonWriter(ms, new JsonWriterOptions { Indented = true });
            
            writer.WriteStartObject();
            
            // Service section
            writer.WriteStartObject("Service");
            writer.WriteString("ListenUrl", "https://0.0.0.0:8443");
            writer.WriteEndObject();
            
            // Database section
            writer.WriteStartObject("Database");
            writer.WriteString("Server", TxtDbServer.Text);
            writer.WriteNumber("Port", int.Parse(TxtDbPort.Text));
            writer.WriteString("Database", TxtDbName.Text);
            writer.WriteString("Instance", "");
            writer.WriteString("Username", TxtDbUser.Text);
            writer.WriteString("PasswordEncrypted", ProtectString(TxtDbPassword.Password));
            writer.WriteBoolean("TrustServerCertificate", ChkTrustCertificate.IsChecked == true);
            writer.WriteBoolean("Encrypted", true);
            writer.WriteNumber("CommandTimeoutSeconds", 60);
            writer.WriteEndObject();
            
            // Security section
            writer.WriteStartObject("Security");
            writer.WriteBoolean("RequireApiKey", true);
            writer.WriteBoolean("EnableHttps", true);
            
            writer.WriteStartObject("Certificate");
            writer.WriteString("Path", string.IsNullOrWhiteSpace(TxtCertPath.Text) ? "" : TxtCertPath.Text);
            writer.WriteString("PasswordEncrypted", "");
            writer.WriteEndObject();
            
            writer.WriteString("ApiKeyEncrypted", ProtectString(generatedApiKey!));
            
            var ipList = TxtIpAllowList.Text.Split(',')
                .Select(ip => ip.Trim())
                .Where(ip => !string.IsNullOrWhiteSpace(ip))
                .ToArray();
            
            writer.WriteStartArray("IpAllowList");
            foreach (var ip in ipList)
            {
                writer.WriteStringValue(ip);
            }
            writer.WriteEndArray();
            
            writer.WriteString("AdminPassphraseHash", HashPassphrase(TxtAdminPassphrase.Password));
            writer.WriteEndObject();
            
            // Logging section
            writer.WriteStartObject("Logging");
            writer.WriteStartObject("LogLevel");
            writer.WriteString("Default", "Information");
            writer.WriteEndObject();
            writer.WriteString("Directory", Path.Combine(ConfigPath, "logs"));
            writer.WriteNumber("RetentionDays", 30);
            writer.WriteEndObject();
            
            writer.WriteEndObject();
            writer.Flush();
            
            var appSettingsJson = Encoding.UTF8.GetString(ms.ToArray());
            await File.WriteAllTextAsync(Path.Combine(ConfigPath, "appsettings.json"), appSettingsJson);
            
            // Copy other configuration files
            await File.WriteAllTextAsync(Path.Combine(ConfigPath, "queries.json"), queriesTemplate);
            await File.WriteAllTextAsync(Path.Combine(ConfigPath, "mapping.json"), mappingTemplate);
            await File.WriteAllTextAsync(Path.Combine(InstallPath, "integration.json"), integrationTemplate);
            
            LogInstall("Configuration files created successfully");
        }

        private async Task<string> LoadEmbeddedResourceAsync(string resourceName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
                throw new Exception($"Resource not found: {resourceName}");
            
            using var reader = new StreamReader(stream);
            return await reader.ReadToEndAsync();
        }

        private string ProtectString(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";
            
            var bytes = Encoding.UTF8.GetBytes(value);
            var protectedBytes = ProtectedData.Protect(bytes, null, DataProtectionScope.LocalMachine);
            return Convert.ToBase64String(protectedBytes);
        }

        private string HashPassphrase(string passphrase)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(passphrase);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        private string GenerateApiKey()
        {
            var bytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }
            return Convert.ToBase64String(bytes);
        }

        private void InstallWindowsService()
        {
            var servicePath = Path.Combine(InstallPath, "SqlSyncService.exe");
            
            // Check if service already exists and remove it
            var checkProcess = RunCommand("sc", "query SqlSyncService");
            if (checkProcess.ExitCode == 0)
            {
                LogInstall("Existing service found, removing...");
                RunCommand("sc", "stop SqlSyncService");
                Thread.Sleep(2000);
                RunCommand("sc", "delete SqlSyncService");
                Thread.Sleep(2000);
            }
            
            // Create service
            var result = RunCommand("sc", $"create SqlSyncService binPath= \"{servicePath}\" start= demand DisplayName= \"SQL Sync Service\"");
            if (result.ExitCode != 0)
                throw new Exception($"Failed to create service: {result.Error}");
            
            RunCommand("sc", "description SqlSyncService \"Provides secure HTTPS API access to SQL Server data\"");
            
            LogInstall("Windows Service installed successfully");
        }

        private void ConfigureFirewall()
        {
            // Remove existing rule
            RunCommand("netsh", "advfirewall firewall delete rule name=\"SqlSyncService HTTPS\"");
            
            // Add new rule
            var result = RunCommand("netsh", "advfirewall firewall add rule name=\"SqlSyncService HTTPS\" dir=in action=allow protocol=TCP localport=8443");
            
            if (result.ExitCode == 0)
                LogInstall("Firewall rule configured successfully");
            else
                LogInstall("Warning: Failed to configure firewall rule");
        }

        private void CreateShortcuts()
        {
            try
            {
                var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                var startMenuPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu), "Programs", "SqlSyncService");
                
                // Create Start Menu folder
                Directory.CreateDirectory(startMenuPath);
                
                // Create desktop shortcut for Admin UI
                var adminExePath = Path.Combine(InstallPath, "Admin", "SqlSyncService.Admin.exe");
                if (File.Exists(adminExePath))
                {
                    var desktopShortcut = Path.Combine(desktopPath, "SqlSyncService Admin.lnk");
                    CreateShortcut(desktopShortcut, adminExePath, "SqlSyncService Admin - Configuration Tool");
                    LogInstall("Desktop shortcut created: SqlSyncService Admin");
                    
                    // Also create Start Menu shortcut
                    var startMenuShortcut = Path.Combine(startMenuPath, "SqlSyncService Admin.lnk");
                    CreateShortcut(startMenuShortcut, adminExePath, "SqlSyncService Admin - Configuration Tool");
                    LogInstall("Start Menu shortcut created");
                }
                else
                {
                    LogInstall("Warning: Admin UI not found, shortcut not created");
                }
                
                // Create Start Menu shortcut for configuration folder
                var configShortcut = Path.Combine(startMenuPath, "Configuration Files.lnk");
                CreateShortcut(configShortcut, ConfigPath, "SqlSyncService Configuration Files");
                
                LogInstall("All shortcuts created successfully");
            }
            catch (Exception ex)
            {
                LogInstall($"Warning: Failed to create shortcuts: {ex.Message}");
            }
        }
        
        private void CreateShortcut(string shortcutPath, string targetPath, string description)
        {
            try
            {
                var shell = Type.GetTypeFromProgID("WScript.Shell");
                if (shell == null) return;
                
                dynamic wsh = Activator.CreateInstance(shell);
                var shortcut = wsh.CreateShortcut(shortcutPath);
                shortcut.TargetPath = targetPath;
                shortcut.Description = description;
                shortcut.WorkingDirectory = Path.GetDirectoryName(targetPath);
                shortcut.Save();
                
                System.Runtime.InteropServices.Marshal.ReleaseComObject(shortcut);
                System.Runtime.InteropServices.Marshal.ReleaseComObject(wsh);
            }
            catch
            {
                // Fallback: create batch file for desktop shortcut if it's for an exe
                if (shortcutPath.Contains("Desktop") && targetPath.EndsWith(".exe"))
                {
                    var batchPath = shortcutPath.Replace(".lnk", ".bat");
                    File.WriteAllText(batchPath, $"@echo off\nstart \"\" \"{targetPath}\"");
                }
            }
        }

        private (int ExitCode, string Output, string Error) RunCommand(string fileName, string arguments)
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };
            
            process.Start();
            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit();
            
            return (process.ExitCode, output, error);
        }

        private void LogInstall(string message)
        {
            Dispatcher.Invoke(() =>
            {
                TxtInstallLog.AppendText(message + Environment.NewLine);
                TxtInstallLog.ScrollToEnd();
                TxtInstallStatus.Text = message;
            });
        }
    }
}
