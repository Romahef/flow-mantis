using System.IO;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
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
        // Show login dialog
        var loginWindow = new LoginWindow(_authService);
        var result = loginWindow.ShowDialog();

        if (result != true)
        {
            Close();
            return;
        }

        // Load configuration
        LoadConfiguration();
    }

    private void LoadConfiguration()
    {
        try
        {
            _settings = _configStore.LoadAppSettings();
            _queries = _configStore.LoadQueries();
            _mapping = _configStore.LoadMapping();

            // Populate UI
            PopulateSecurityTab();
            PopulateDatabaseTab();
            PopulateQueriesTab();
            PopulateMappingTab();
            PopulateAboutTab();
        }
        catch (Exception ex)
        {
            ShowError($"Failed to load configuration: {ex.Message}");
        }
    }

    private void PopulateSecurityTab()
    {
        // IP Allow List
        IpListBox.ItemsSource = _settings.Security.IpAllowList;

        // Certificate Path
        CertPathTextBox.Text = _settings.Security.Certificate.Path;
    }

    private void PopulateDatabaseTab()
    {
        DbServerTextBox.Text = _settings.Database.Server;
        DbPortTextBox.Text = _settings.Database.Port.ToString();
        DbNameTextBox.Text = _settings.Database.Database;
        DbInstanceTextBox.Text = _settings.Database.Instance;
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
    private async void TestConnection_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Update settings from UI
            _settings.Database.Server = DbServerTextBox.Text;
            _settings.Database.Port = int.Parse(DbPortTextBox.Text);
            _settings.Database.Database = DbNameTextBox.Text;
            _settings.Database.Instance = DbInstanceTextBox.Text;

            var connectionFactory = new ConnectionFactory(_settings, 
                Microsoft.Extensions.Logging.Abstractions.NullLogger<ConnectionFactory>.Instance);
            
            var (success, message) = await connectionFactory.TestConnectionAsync();

            if (success)
            {
                ShowSuccess($"✓ {message}");
            }
            else
            {
                ShowError($"✗ {message}");
            }
        }
        catch (Exception ex)
        {
            ShowError($"Connection test failed: {ex.Message}");
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
        if (sender is FrameworkElement element && element.DataContext != null)
        {
            var queryView = element.DataContext;
            var query = queryView.GetType().GetProperty("Query")?.GetValue(queryView) as QueryDefinition;
            
            if (query != null)
            {
                var dialog = new QueryEditorWindow(query);
                if (dialog.ShowDialog() == true)
                {
                    PopulateQueriesTab();
                }
            }
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
