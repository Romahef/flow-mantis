using System.IO;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SqlSyncService.Config;
using SqlSyncService.Admin.Services;

namespace SqlSyncService.Admin;

public partial class App : Application
{
    private ServiceProvider? _serviceProvider;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Add global exception handler for unhandled exceptions
        DispatcherUnhandledException += OnDispatcherUnhandledException;

        try
        {
            // Setup dependency injection
            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();

            // Show main window
            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to start application:\n\n{ex.Message}", 
                "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    private void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        MessageBox.Show($"An unexpected error occurred:\n\n{e.Exception.Message}", 
            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // Logging
        services.AddLogging(configure =>
        {
            configure.AddDebug();
            configure.AddConsole();
        });

        // Configuration
        var configDirectory = GetConfigDirectory();
        services.AddSingleton(sp => new ConfigStore(
            configDirectory,
            sp.GetRequiredService<ILogger<ConfigStore>>()));

        // Services
        services.AddSingleton<AdminAuthService>();
        services.AddSingleton<AdminApplyService>();

        // Windows
        services.AddTransient<MainWindow>();
        services.AddTransient<LoginWindow>();
    }

    private static string GetConfigDirectory()
    {
        var customPath = Environment.GetEnvironmentVariable("SQLSYNC_CONFIG_DIR");
        if (!string.IsNullOrEmpty(customPath) && Directory.Exists(customPath))
        {
            return customPath;
        }

        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "SqlSyncService");
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }
}
