using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using EBDC.Services;
using EBDC.Services.Interfaces;
using Application = System.Windows.Application;
using IDisposable = System.IDisposable;

namespace EBDC
{
    public partial class App : Application
    {
        private ServiceProvider? _serviceProvider;

        protected override void OnStartup(StartupEventArgs e)
        {
            var services = new ServiceCollection();

            ConfigureServices(services);

            _serviceProvider = services.BuildServiceProvider();

            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }

        private void ConfigureServices(ServiceCollection services)
        {
            services.AddLogging(configure => configure.AddConsole());
            
            services.AddSingleton<IDownloader, YtDlpDownloader>();
            services.AddSingleton<IDownloadManager, DownloadManager>();
            services.AddSingleton<ISystemMonitor, SystemMonitor>();
            services.AddSingleton<UpdateService>();
            services.AddSingleton<MainWindow>();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            if (_serviceProvider is IDisposable disposable)
            {
                disposable.Dispose();
            }

            base.OnExit(e);
        }
    }
}
