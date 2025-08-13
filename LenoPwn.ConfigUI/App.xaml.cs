using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Windows;

namespace LenoPwn.ConfigUI
{
    public partial class App : Application
    {
        private static readonly IHost _host = Host
            .CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                services.AddHostedService<ApplicationHostService>();
                services.AddSingleton<MainWindow>();
            }).Build();

        public static T GetService<T>() where T : class
        {
            return _host.Services.GetRequiredService<T>();
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            await _host.StartAsync();
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            await _host.StopAsync();
            _host.Dispose();
        }
    }
}