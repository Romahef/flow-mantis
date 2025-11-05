using System.IO;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using Microsoft.Data.SqlClient;
using SqlSyncService.Config;
using SqlSyncService.Admin.Services;
using SqlSyncService.Database;
using Microsoft.Extensions.Logging;

namespace SqlSyncService.Admin;

public partial class MainWindow : Window
{
    private readonly ConfigStore _configStore;
    private readonly AdminAuthService _authService;
    private readonly AdminApplyService _applyService;
    private readonly ILogger<MainWindow> _logger;

    private AppSettings _settings = new();
    private QueriesConfig _queries = new();
    private MappingConfig _mapping = new();

    public MainWindow(
        ConfigStore configStore,
        AdminAuthService authService,
        AdminApplyService applyService,
        ILogger<MainWindow> logger)
    {
        InitializeComponent();
        _configStore = configStore;
        _authService = authService;
        _applyService = applyService;
        _logger = logger;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            // Show login dialog
            var loginWindow = new LoginWindow(_authService);
            var result = loginWindow.ShowDialog();

            if (result != true)
            {
                _logger.LogInformation("User cancelled login");
                Application.Current.Shutdown();
                return;
            }

            // Load configuration
            _logger.LogInformation("User authenticated, loading configuration");
            LoadConfiguration();
            _logger.LogInformation("Configuration loaded successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error during startup");
            MessageBox.Show($"Fatal error during startup:\n\n{ex.Message}\n\nStack Trace:\n{ex.StackTrace}", "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Application.Current.Shutdown();
        }
    }

    private void LoadConfiguration()
    {
        try
        {
            _logger.LogInformation("Loading app settings");
            _settings = _configStore.LoadAppSettings();
            
            _logger.LogInformation("Loading queries");
            _queries = _configStore.LoadQueries();
            
            _logger.LogInformation("Loading mapping");
            _mapping = _configStore.LoadMapping();

            // Populate UI
            _logger.LogInformation("Populating Security tab");
            PopulateSecurityTab();
            
            _logger.LogInformation("Populating Database tab");
            PopulateDatabaseTab();
            
            _logger.LogInformation("Populating Queries tab");
            PopulateQueriesTab();
            
            _logger.LogInformation("Populating Mapping tab");
            PopulateMappingTab();
            
            _logger.LogInformation("Populating About tab");
            PopulateAboutTab();
            
            _logger.LogInformation("All tabs populated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load configuration");
            MessageBox.Show($"Failed to load configuration:\n\n{ex.Message}\n\nStack Trace:\n{ex.StackTrace}", "Configuration Error", MessageBoxButton.OK, MessageBoxImage.Error);
            
            // Re-throw so the outer handler can decide whether to close the app
            throw;
        }
    }

    private void PopulateSecurityTab()
    {
        // IP Allow List
        IpListBox.ItemsSource = _settings.Security.IpAllowList;

        // Certificate Path
        CertPathTextBox.Text = _settings.Security.Certificate.Path;
        
        // API Port - extract from Service.ListenUrl
        try
        {
            var listenUrl = _settings.Service.ListenUrl ?? "http://localhost:8080";
            var uri = new Uri(listenUrl);
            TxtApiPortAdmin.Text = uri.Port.ToString();
        }
        catch
        {
            TxtApiPortAdmin.Text = "8080"; // Default
        }
    }

    private void PopulateDatabaseTab()
    {
        DbServerTextBox.Text = _settings.Database.Server;
        DbPortTextBox.Text = _settings.Database.Port.ToString();
        DbNameTextBox.Text = _settings.Database.Database;
        DbInstanceTextBox.Text = _settings.Database.Instance;
        
        // Display decrypted username
        try
        {
            var username = ConfigStore.Secrets.GetDatabaseUsername(_settings);
            DbUsernameTextBox.Text = username;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to decrypt database username");
            DbUsernameTextBox.Text = "";
        }
        
        // Password field stays empty for security (user can enter new password if needed)
        DbPasswordBox.Password = "";
    }

    private void PopulateQueriesTab()
    {
        var queryViews = _queries.Queries.Select(q => new
        {
            Query = q,
            q.Name,
            q.Paginable,
            q.PaginationMode,
            OrderByOrKeys = q.PaginationMode == "Offset" 
                ? q.OrderBy 
                : string.Join(", ", q.KeyColumns)
        }).ToList();

        QueriesDataGrid.ItemsSource = queryViews;
    }

    private void PopulateMappingTab()
    {
        var mappingItems = _mapping.Routes.Select(r =>
        {
            var queries = string.Join("\n", r.Queries.Select(q => 
                $"  • {q.QueryName} → {q.TargetArray}"));
            return $"Endpoint: {r.Endpoint}\n{queries}";
        }).ToList();

        MappingListBox.ItemsSource = mappingItems;
    }

    private void PopulateAboutTab()
    {
        ListenUrlText.Text = $"Listen Address: {_settings.Service.ListenUrl}";
        
        var configDir = Environment.GetEnvironmentVariable("SQLSYNC_CONFIG_DIR") 
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "SqlSyncService");
        ConfigDirText.Text = $"Config Directory: {configDir}";
    }

    // Security Tab Handlers
    private void RotateApiKey_Click(object sender, RoutedEventArgs e)
    {
        var newApiKey = SecretsProtector.GenerateApiKey();
        _settings.Security.ApiKeyEncrypted = SecretsProtector.Protect(newApiKey);

        var result = MessageBox.Show(
            $"New API Key generated:\n\n{newApiKey}\n\n" +
            "SAVE THIS KEY SECURELY!\n\n" +
            "This key will be needed by all clients accessing the API.\n\n" +
            "Click OK to continue, or Cancel to generate a different key.",
            "New API Key Generated",
            MessageBoxButton.OKCancel,
            MessageBoxImage.Information);

        if (result == MessageBoxResult.OK)
        {
            ShowSuccess("API key rotated successfully. Remember to update all clients!");
        }
        else
        {
            LoadConfiguration(); // Reload to revert
        }
    }
    
    private void UpdateApiPort_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Validate port
            if (!int.TryParse(TxtApiPortAdmin.Text, out int port) || port < 1024 || port > 65535)
            {
                ShowError("Please enter a valid port number (1024-65535)");
                return;
            }
            
            // Extract protocol from current Service.ListenUrl
            var currentUrl = _settings.Service.ListenUrl ?? "http://localhost:8080";
            var currentUri = new Uri(currentUrl);
            var protocol = currentUri.Scheme; // http or https
            
            // Update Service.ListenUrl with new port
            _settings.Service.ListenUrl = $"{protocol}://0.0.0.0:{port}";
            
            // Save configuration
            _configStore.SaveAppSettings(_settings);
            
            ShowSuccess($"API port updated to {port}. Please restart the SqlSyncService for changes to take effect.");
        }
        catch (Exception ex)
        {
            ShowError($"Failed to update API port: {ex.Message}");
        }
    }

    private void AddIp_Click(object sender, RoutedEventArgs e)
    {
        var ip = NewIpTextBox.Text.Trim();
        if (string.IsNullOrEmpty(ip))
        {
            ShowError("Please enter an IP address");
            return;
        }

        if (!System.Net.IPAddress.TryParse(ip, out _))
        {
            ShowError("Invalid IP address format");
            return;
        }

        if (_settings.Security.IpAllowList.Contains(ip))
        {
            ShowError("IP address already in list");
            return;
        }

        _settings.Security.IpAllowList.Add(ip);
        IpListBox.ItemsSource = null;
        IpListBox.ItemsSource = _settings.Security.IpAllowList;
        NewIpTextBox.Clear();
    }

    private void NewIpTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            AddIp_Click(sender, e);
        }
    }

    private void RemoveIp_Click(object sender, RoutedEventArgs e)
    {
        if (IpListBox.SelectedItem is string selectedIp)
        {
            _settings.Security.IpAllowList.Remove(selectedIp);
            IpListBox.ItemsSource = null;
            IpListBox.ItemsSource = _settings.Security.IpAllowList;
        }
    }

    private void BrowseCert_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Certificate Files (*.pfx)|*.pfx|All Files (*.*)|*.*",
            Title = "Select Certificate File"
        };

        if (dialog.ShowDialog() == true)
        {
            CertPathTextBox.Text = dialog.FileName;
            _settings.Security.Certificate.Path = dialog.FileName;
        }
    }

    private void ValidateCert_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!File.Exists(_settings.Security.Certificate.Path))
            {
                ShowError($"Certificate file not found: {_settings.Security.Certificate.Path}");
                return;
            }

            var certPassword = ConfigStore.Secrets.GetCertificatePassword(_settings);
            using var cert = new System.Security.Cryptography.X509Certificates.X509Certificate2(
                _settings.Security.Certificate.Path,
                certPassword,
                System.Security.Cryptography.X509Certificates.X509KeyStorageFlags.MachineKeySet);

            if (cert.NotAfter < DateTime.Now)
            {
                ShowError($"Certificate expired on {cert.NotAfter:yyyy-MM-dd}");
            }
            else if (cert.NotBefore > DateTime.Now)
            {
                ShowError($"Certificate not valid until {cert.NotBefore:yyyy-MM-dd}");
            }
            else
            {
                ShowSuccess($"Certificate is valid until {cert.NotAfter:yyyy-MM-dd}\nSubject: {cert.Subject}");
            }
        }
        catch (Exception ex)
        {
            ShowError($"Certificate validation failed: {ex.Message}");
        }
    }

    // Database Tab Handlers
    private void SaveCredentials_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(DbUsernameTextBox.Text))
            {
                ShowError("Username cannot be empty");
                return;
            }

            // Update server settings
            _settings.Database.Server = DbServerTextBox.Text;
            _settings.Database.Port = int.Parse(DbPortTextBox.Text);
            _settings.Database.Database = DbNameTextBox.Text;
            _settings.Database.Instance = DbInstanceTextBox.Text;
            
            // Update username (always encrypt from UI value)
            _settings.Database.UsernameEncrypted = SecretsProtector.Protect(DbUsernameTextBox.Text);
            
            // Update password only if a new one is provided
            if (!string.IsNullOrEmpty(DbPasswordBox.Password))
            {
                _settings.Database.PasswordEncrypted = SecretsProtector.Protect(DbPasswordBox.Password);
            }
            
            // Save configuration
            _configStore.SaveAppSettings(_settings);
            
            ShowSuccess("✓ Database credentials saved successfully! Please restart the SqlSyncService for changes to take effect.");
            _logger.LogInformation("Database credentials updated");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save credentials");
            ShowError($"Failed to save credentials: {ex.Message}");
        }
    }
    
    private async void TestConnection_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _logger.LogInformation("Starting database connection test");
            
            // Get credentials from UI or encrypted storage
            string username, password;
            
            // If username is in UI, use it; otherwise try to decrypt from config
            if (!string.IsNullOrWhiteSpace(DbUsernameTextBox.Text))
            {
                username = DbUsernameTextBox.Text;
            }
            else if (!string.IsNullOrWhiteSpace(_settings.Database.UsernameEncrypted))
            {
                try
                {
                    username = ConfigStore.Secrets.GetDatabaseUsername(_settings);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to decrypt username");
                    ShowError("Failed to decrypt username from configuration. Please enter credentials above.");
                    return;
                }
            }
            else
            {
                ShowError("Username is not configured. Please enter username and password above, then click 'Save Credentials'.");
                return;
            }
            
            // If password is in UI, use it; otherwise try to decrypt from config
            if (!string.IsNullOrEmpty(DbPasswordBox.Password))
            {
                password = DbPasswordBox.Password;
            }
            else if (!string.IsNullOrWhiteSpace(_settings.Database.PasswordEncrypted))
            {
                try
                {
                    password = ConfigStore.Secrets.GetDatabasePassword(_settings);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to decrypt password");
                    ShowError("Failed to decrypt password from configuration. Please enter credentials above.");
                    return;
                }
            }
            else
            {
                ShowError("Password is not configured. Please enter username and password above, then click 'Save Credentials'.");
                return;
            }
            
            _logger.LogInformation($"Testing connection with username: {username}");
            
            // Build connection string directly for testing
            var server = DbServerTextBox.Text;
            var port = int.Parse(DbPortTextBox.Text);
            var database = DbNameTextBox.Text;
            var instance = DbInstanceTextBox.Text;
            
            var dataSource = string.IsNullOrWhiteSpace(instance)
                ? $"{server},{port}"
                : $"{server}\\{instance}";
            
            var connectionString = $"Data Source={dataSource};Initial Catalog={database};" +
                                 $"User ID={username};Password={password};" +
                                 $"TrustServerCertificate=True;Encrypt=True;ConnectTimeout=10";
            
            _logger.LogInformation($"Testing connection to {dataSource}/{database}");
            
            using var connection = new Microsoft.Data.SqlClient.SqlConnection(connectionString);
            await connection.OpenAsync();
            
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT @@VERSION";
            command.CommandTimeout = 5;
            
            var version = await command.ExecuteScalarAsync();
            
            _logger.LogInformation("Connection test successful");
            ShowSuccess($"✓ Connection successful!\n\nServer version: {version}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Connection test failed");
            ShowError($"✗ Connection test failed:\n\n{ex.Message}");
        }
    }

    // Query Tab Handlers
    private void AddQuery_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new QueryEditorWindow(new QueryDefinition { Name = "NewQuery" });
        if (dialog.ShowDialog() == true && dialog.Query != null)
        {
            _queries.Queries.Add(dialog.Query);
            PopulateQueriesTab();
        }
    }

    private void EditQuery_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is FrameworkElement element && element.DataContext != null)
            {
                var queryView = element.DataContext;
                var queryProperty = queryView.GetType().GetProperty("Query");
                
                if (queryProperty == null)
                {
                    _logger.LogError("Query property not found in DataContext");
                    ShowError("Failed to access query data. Please try again.");
                    return;
                }
                
                var query = queryProperty.GetValue(queryView) as QueryDefinition;
                
                if (query == null)
                {
                    _logger.LogError("Query property returned null");
                    ShowError("Failed to load query data. Please try again.");
                    return;
                }
                
                var dialog = new QueryEditorWindow(query);
                if (dialog.ShowDialog() == true)
                {
                    PopulateQueriesTab();
                }
            }
            else
            {
                _logger.LogError("EditQuery_Click: sender or DataContext is null");
                ShowError("Failed to access query data. Please try again.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error opening query editor");
            ShowError($"Error opening query editor: {ex.Message}");
        }
    }

    private void DeleteQuery_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element && element.DataContext != null)
        {
            var queryView = element.DataContext;
            var query = queryView.GetType().GetProperty("Query")?.GetValue(queryView) as QueryDefinition;
            
            if (query != null)
            {
                var result = MessageBox.Show(
                    $"Are you sure you want to delete the query '{query.Name}'?",
                    "Confirm Delete",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    _queries.Queries.Remove(query);
                    PopulateQueriesTab();
                }
            }
        }
    }

    private void QueriesDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (QueriesDataGrid.SelectedItem != null)
        {
            EditQuery_Click(QueriesDataGrid, e);
        }
    }

    // Save Configuration
    private async void SaveConfig_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Update settings from UI
            _settings.Database.Server = DbServerTextBox.Text;
            _settings.Database.Port = int.Parse(DbPortTextBox.Text);
            _settings.Database.Database = DbNameTextBox.Text;
            _settings.Database.Instance = DbInstanceTextBox.Text;
            _settings.Security.Certificate.Path = CertPathTextBox.Text;

            // Validate and apply
            var (success, errors) = await _applyService.ApplyConfigurationAsync(
                _settings, _queries, _mapping);

            if (success)
            {
                ShowSuccess("✓ Configuration saved successfully! Restart the service for changes to take effect.");
            }
            else
            {
                ShowError($"Configuration validation failed:\n\n{string.Join("\n", errors)}");
            }
        }
        catch (Exception ex)
        {
            ShowError($"Failed to save configuration: {ex.Message}");
        }
    }

    // Alert Management
    private void ShowSuccess(string message)
    {
        AlertText.Text = message;
        AlertPanel.Background = (System.Windows.Media.Brush)FindResource("SuccessBrush");
        AlertPanel.Visibility = Visibility.Visible;
    }

    private void ShowError(string message)
    {
        AlertText.Text = message;
        AlertPanel.Background = (System.Windows.Media.Brush)FindResource("DangerBrush");
        AlertPanel.Visibility = Visibility.Visible;
        MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
    }

    private void CloseAlert_Click(object sender, RoutedEventArgs e)
    {
        AlertPanel.Visibility = Visibility.Collapsed;
    }
}
