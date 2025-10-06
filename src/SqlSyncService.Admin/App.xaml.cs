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

        // Setup dependency injection
        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();

        // Show main window
        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();
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
