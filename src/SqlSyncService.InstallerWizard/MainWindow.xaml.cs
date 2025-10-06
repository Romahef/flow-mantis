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
            
            // Check for existing installation
            Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (IsExistingInstallationDetected())
            {
                var result = MessageBox.Show(
                    "An existing installation of SqlSyncService has been detected.\n\n" +
                    "Would you like to uninstall the previous version before installing?\n\n" +
                    "Note: Your configuration files will be preserved and can be reused.",
                    "Existing Installation Detected",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    // Show installing page for uninstallation
                    ShowPage(3);
                    BtnNext.IsEnabled = false;
                    BtnBack.IsEnabled = false;
                    BtnCancel.IsEnabled = false;

                    LogInstall("Uninstalling previous version...");
                    await Task.Run(() => UninstallPreviousVersion());
                    LogInstall("Previous version uninstalled successfully!");
                    LogInstall("");

                    await Task.Delay(2000);

                    // Go back to welcome page
                    ShowPage(0);
                    BtnNext.IsEnabled = true;
                    BtnBack.IsEnabled = false;
                    BtnCancel.IsEnabled = true;

                    MessageBox.Show(
                        "Previous installation has been removed.\n\n" +
                        "You can now proceed with the new installation.",
                        "Uninstall Complete",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                else if (result == MessageBoxResult.Cancel)
                {
                    Application.Current.Shutdown();
                }
                // If No, continue with installation (might fail if service is running)
            }
        }

        private bool IsExistingInstallationDetected()
        {
            // Check if service exists
            try
            {
                var serviceCheck = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "sc",
                        Arguments = "query SqlSyncService",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    }
                };
                serviceCheck.Start();
                serviceCheck.WaitForExit();

                if (serviceCheck.ExitCode == 0)
                    return true;
            }
            catch { }

            // Check if installation directory exists
            if (Directory.Exists(InstallPath) && Directory.GetFiles(InstallPath, "SqlSyncService.exe", SearchOption.TopDirectoryOnly).Length > 0)
                return true;

            return false;
        }

        private void UninstallPreviousVersion()
        {
            try
            {
                // Step 1: Stop the service
                LogInstall("[1/4] Stopping service...");
                try
                {
                    RunCommand("sc", "stop SqlSyncService");
                    Thread.Sleep(2000);
                    LogInstall("Service stopped");
                }
                catch
                {
                    LogInstall("Service was not running");
                }

                // Step 2: Delete the service
                LogInstall("[2/4] Removing service...");
                try
                {
                    RunCommand("sc", "delete SqlSyncService");
                    Thread.Sleep(2000);
                    LogInstall("Service removed");
                }
                catch (Exception ex)
                {
                    LogInstall($"Warning: {ex.Message}");
                }

                // Step 3: Remove firewall rule
                LogInstall("[3/4] Removing firewall rule...");
                try
                {
                    RunCommand("netsh", "advfirewall firewall delete rule name=\"SqlSyncService HTTPS\"");
                    LogInstall("Firewall rule removed");
                }
                catch
                {
                    LogInstall("No firewall rule found");
                }

                // Step 4: Remove shortcuts
                LogInstall("[4/4] Removing shortcuts...");
                try
                {
                    var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                    var shortcuts = new[]
                    {
                        Path.Combine(desktopPath, "SqlSyncService Admin.lnk"),
                        Path.Combine(desktopPath, "SqlSyncService Admin.bat")
                    };

                    foreach (var shortcut in shortcuts)
                    {
                        if (File.Exists(shortcut))
                        {
                            File.Delete(shortcut);
                            LogInstall($"Removed: {Path.GetFileName(shortcut)}");
                        }
                    }

                    var startMenuPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu), "Programs", "SqlSyncService");
                    if (Directory.Exists(startMenuPath))
                    {
                        Directory.Delete(startMenuPath, true);
                        LogInstall("Removed Start Menu folder");
                    }

                    LogInstall("Shortcuts removed");
                }
                catch (Exception ex)
                {
                    LogInstall($"Warning: {ex.Message}");
                }

                // Remove old files (but keep configuration)
                LogInstall("Removing old files (preserving configuration)...");
                
                // Kill any running Admin UI processes first
                try
                {
                    var adminProcesses = Process.GetProcessesByName("SqlSyncService.Admin");
                    foreach (var proc in adminProcesses)
                    {
                        try
                        {
                            proc.Kill();
                            proc.WaitForExit(2000);
                            LogInstall("Stopped running Admin UI");
                        }
                        catch { }
                    }
                }
                catch { }
                
                Thread.Sleep(1000); // Give processes time to fully exit
                
                try
                {
                    if (Directory.Exists(InstallPath))
                    {
                        // Try multiple times if files are locked
                        int attempts = 0;
                        bool deleted = false;
                        
                        while (attempts < 3 && !deleted)
                        {
                            try
                            {
                                Directory.Delete(InstallPath, true);
                                deleted = true;
                                LogInstall("Old files removed successfully");
                            }
                            catch
                            {
                                attempts++;
                                if (attempts < 3)
                                {
                                    Thread.Sleep(1000);
                                    LogInstall($"Retrying file deletion (attempt {attempts + 1}/3)...");
                                }
                            }
                        }
                        
                        if (!deleted)
                        {
                            LogInstall($"Warning: Could not remove all files after 3 attempts");
                            LogInstall("Trying to remove Admin folder specifically...");
                            
                            // Try to at least remove the Admin folder
                            var adminPath = Path.Combine(InstallPath, "Admin");
                            if (Directory.Exists(adminPath))
                            {
                                try
                                {
                                    Directory.Delete(adminPath, true);
                                    LogInstall("Admin UI folder removed");
                                }
                                catch (Exception adminEx)
                                {
                                    LogInstall($"Warning: Could not remove Admin UI: {adminEx.Message}");
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogInstall($"Warning: Error during file removal: {ex.Message}");
                    LogInstall("Installation will continue...");
                }
            }
            catch (Exception ex)
            {
                LogInstall($"Error during uninstallation: {ex.Message}");
            }
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
            
            // Validate Let's Encrypt fields
            if (RbHttpsLetsEncrypt.IsChecked == true)
            {
                if (string.IsNullOrWhiteSpace(TxtDomainName.Text) || TxtDomainName.Text.Contains("yourdomain"))
                {
                    MessageBox.Show("Please enter a valid domain name for Let's Encrypt.", 
                        "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }
                
                if (string.IsNullOrWhiteSpace(TxtLetsEncryptEmail.Text) || TxtLetsEncryptEmail.Text.Contains("yourdomain"))
                {
                    MessageBox.Show("Please enter a valid email address for Let's Encrypt notifications.", 
                        "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }
                
                if (ChkAcceptTerms.IsChecked != true)
                {
                    MessageBox.Show("You must accept the Let's Encrypt Terms of Service to continue.", 
                        "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }
            }
            
            // Validate certificate file if custom certificate is selected
            if (RbHttpsCustomCert.IsChecked == true)
            {
                if (string.IsNullOrWhiteSpace(TxtCertPath.Text))
                {
                    MessageBox.Show("Please select a certificate file (.pfx) or choose a different security mode.", 
                        "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }
                
                if (!File.Exists(TxtCertPath.Text))
                {
                    MessageBox.Show("The selected certificate file does not exist.", 
                        "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }
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

        private void SecurityMode_Changed(object sender, RoutedEventArgs e)
        {
            // Show appropriate panel based on selection
            if (RbHttpsLetsEncrypt != null && RbHttpsCustomCert != null)
            {
                PnlLetsEncrypt.Visibility = RbHttpsLetsEncrypt.IsChecked == true 
                    ? Visibility.Visible 
                    : Visibility.Collapsed;
                    
                PnlCertificateUpload.Visibility = RbHttpsCustomCert.IsChecked == true 
                    ? Visibility.Visible 
                    : Visibility.Collapsed;
            }
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = e.Uri.AbsoluteUri,
                UseShellExecute = true
            });
            e.Handled = true;
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
            
            // Determine security mode
            bool enableHttps = RbHttpsSelfSigned.IsChecked == true || RbHttpsCustomCert.IsChecked == true || RbHttpsLetsEncrypt.IsChecked == true;
            string protocol = enableHttps ? "https" : "http";
            int port = enableHttps ? 8443 : 8080;
            string certPath = "";
            string certPassword = "";
            
            // Generate self-signed certificate if needed
            if (RbHttpsSelfSigned.IsChecked == true)
            {
                LogInstall("Generating self-signed certificate...");
                certPath = Path.Combine(ConfigPath, "self-signed-cert.pfx");
                certPassword = GenerateApiKey(); // Use strong random password
                GenerateSelfSignedCertificate(certPath, certPassword);
                LogInstall($"Self-signed certificate created: {certPath}");
            }
            else if (RbHttpsLetsEncrypt.IsChecked == true)
            {
                LogInstall("Obtaining Let's Encrypt certificate...");
                LogInstall($"Domain: {TxtDomainName.Text}");
                certPath = Path.Combine(ConfigPath, "letsencrypt-cert.pfx");
                certPassword = GenerateApiKey();
                
                try
                {
                    await ObtainLetsEncryptCertificate(TxtDomainName.Text, TxtLetsEncryptEmail.Text, certPath, certPassword);
                    LogInstall($"Let's Encrypt certificate obtained successfully!");
                }
                catch (Exception ex)
                {
                    LogInstall($"Warning: Let's Encrypt certificate could not be obtained: {ex.Message}");
                    LogInstall("Falling back to self-signed certificate...");
                    certPath = Path.Combine(ConfigPath, "self-signed-cert.pfx");
                    GenerateSelfSignedCertificate(certPath, certPassword);
                    LogInstall("Self-signed certificate created as fallback");
                }
            }
            else if (RbHttpsCustomCert.IsChecked == true)
            {
                certPath = TxtCertPath.Text;
                certPassword = TxtCertPassword.Password;
            }
            
            // Service section
            writer.WriteStartObject("Service");
            writer.WriteString("ListenUrl", $"{protocol}://0.0.0.0:{port}");
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
            writer.WriteBoolean("EnableHttps", enableHttps);
            
            writer.WriteStartObject("Certificate");
            writer.WriteString("Path", certPath);
            writer.WriteString("PasswordEncrypted", string.IsNullOrEmpty(certPassword) ? "" : ProtectString(certPassword));
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
            writer.WriteEndObject();
            
            // Admin section
            writer.WriteStartObject("Admin");
            writer.WriteString("ListenUrl", "https://localhost:9443");
            writer.WriteString("PassphraseEncrypted", ProtectString(HashPassphrase(TxtAdminPassphrase.Password)));
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

        private void GenerateSelfSignedCertificate(string certPath, string password)
        {
            try
            {
                // Use PowerShell to generate self-signed certificate
                var psScript = $@"
                    $cert = New-SelfSignedCertificate -DnsName 'localhost', '127.0.0.1' -CertStoreLocation 'Cert:\LocalMachine\My' -NotAfter (Get-Date).AddYears(10) -KeyAlgorithm RSA -KeyLength 2048
                    $certPassword = ConvertTo-SecureString -String '{password}' -Force -AsPlainText
                    Export-PfxCertificate -Cert $cert -FilePath '{certPath}' -Password $certPassword
                    Remove-Item -Path ""Cert:\LocalMachine\My\$($cert.Thumbprint)"" -Force
                ";
                
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{psScript}\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };
                
                process.Start();
                process.WaitForExit();
                
                if (process.ExitCode != 0)
                {
                    var error = process.StandardError.ReadToEnd();
                    throw new Exception($"Certificate generation failed: {error}");
                }
            }
            catch (Exception ex)
            {
                LogInstall($"Warning: Could not generate self-signed certificate: {ex.Message}");
                throw;
            }
        }

        private async Task ObtainLetsEncryptCertificate(string domain, string email, string certPath, string password)
        {
            try
            {
                // Download win-acme if not present
                var wacsPath = Path.Combine(InstallPath, "wacs");
                if (!Directory.Exists(wacsPath))
                {
                    LogInstall("Downloading win-acme (ACME client)...");
                    await DownloadWinAcme(wacsPath);
                }
                
                var wacsExe = Path.Combine(wacsPath, "wacs.exe");
                
                // Create win-acme command
                // Using standalone mode with manual plugin for simple HTTP-01 challenge
                var wacsArgs = $"--source manual --host {domain} --emailaddress {email} " +
                              $"--accepttos --store pemfiles --pemfilespath \"{ConfigPath}\" " +
                              $"--installation script --script \"powershell.exe\" " +
                              $"--scriptparameters \"Exit 0\" --closeonfinish --noninteractive";
                
                LogInstall($"Running ACME challenge for {domain}...");
                LogInstall("Note: This requires port 80 to be accessible from the internet");
                
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = wacsExe,
                        Arguments = wacsArgs,
                        WorkingDirectory = wacsPath,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };
                
                process.OutputDataReceived += (s, e) => {
                    if (!string.IsNullOrWhiteSpace(e.Data))
                        LogInstall($"  {e.Data}");
                };
                
                process.Start();
                process.BeginOutputReadLine();
                await Task.Run(() => process.WaitForExit());
                
                if (process.ExitCode != 0)
                {
                    throw new Exception($"win-acme failed with exit code {process.ExitCode}");
                }
                
                // Convert PEM files to PFX
                LogInstall("Converting certificate to PFX format...");
                var pemCert = Path.Combine(ConfigPath, $"{domain}-chain.pem");
                var pemKey = Path.Combine(ConfigPath, $"{domain}-key.pem");
                
                if (!File.Exists(pemCert) || !File.Exists(pemKey))
                {
                    throw new Exception("Certificate files not found after ACME challenge");
                }
                
                // Use OpenSSL or PowerShell to convert PEM to PFX
                var convertScript = $@"
                    $cert = Get-Content '{pemCert}' -Raw
                    $key = Get-Content '{pemKey}' -Raw
                    $certPassword = ConvertTo-SecureString -String '{password}' -Force -AsPlainText
                    
                    # Create temp PEM file with both cert and key
                    $combined = $cert + ""`n"" + $key
                    $tempPem = '{Path.Combine(ConfigPath, "temp.pem")}'
                    Set-Content -Path $tempPem -Value $combined
                    
                    # Convert to PFX using certutil or openssl (if available)
                    # For now, we'll use a PowerShell approach
                    $certObj = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2($tempPem, '', 'Exportable')
                    $certBytes = $certObj.Export('Pfx', '{password}')
                    [System.IO.File]::WriteAllBytes('{certPath}', $certBytes)
                    
                    Remove-Item $tempPem -Force
                ";
                
                var convertProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{convertScript}\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };
                
                convertProcess.Start();
                await Task.Run(() => convertProcess.WaitForExit());
                
                if (!File.Exists(certPath))
                {
                    throw new Exception("Failed to convert certificate to PFX format");
                }
                
                // Setup auto-renewal (create scheduled task)
                LogInstall("Setting up automatic certificate renewal...");
                CreateCertRenewalTask(wacsExe, wacsPath);
            }
            catch (Exception ex)
            {
                LogInstall($"Let's Encrypt error: {ex.Message}");
                throw;
            }
        }

        private async Task DownloadWinAcme(string targetPath)
        {
            // For production, download win-acme from GitHub releases
            // For now, we'll provide instructions
            Directory.CreateDirectory(targetPath);
            
            var readmePath = Path.Combine(targetPath, "README.txt");
            await File.WriteAllTextAsync(readmePath, 
                "Win-ACME download required\n\n" +
                "To use Let's Encrypt, download win-acme from:\n" +
                "https://github.com/win-acme/win-acme/releases/latest\n\n" +
                "Extract wacs.exe to this directory.");
            
            throw new Exception("Win-ACME not found. Please download it manually or use a different security mode.");
        }

        private void CreateCertRenewalTask(string wacsExe, string wacsPath)
        {
            try
            {
                // Create a scheduled task to run win-acme renewal daily
                var taskName = "SqlSyncService-CertRenewal";
                var taskScript = $@"
                    $action = New-ScheduledTaskAction -Execute '{wacsExe}' -Argument '--renew --baseuri https://acme-v02.api.letsencrypt.org/' -WorkingDirectory '{wacsPath}'
                    $trigger = New-ScheduledTaskTrigger -Daily -At 3am
                    $principal = New-ScheduledTaskPrincipal -UserId 'SYSTEM' -LogonType ServiceAccount -RunLevel Highest
                    $settings = New-ScheduledTaskSettingsSet -StartWhenAvailable -DontStopOnIdleEnd
                    Register-ScheduledTask -TaskName '{taskName}' -Action $action -Trigger $trigger -Principal $principal -Settings $settings -Force
                ";
                
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{taskScript}\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };
                
                process.Start();
                process.WaitForExit();
                
                if (process.ExitCode == 0)
                {
                    LogInstall("Certificate auto-renewal task created successfully");
                }
            }
            catch (Exception ex)
            {
                LogInstall($"Warning: Could not create renewal task: {ex.Message}");
            }
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
