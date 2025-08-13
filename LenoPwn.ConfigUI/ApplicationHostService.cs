using Microsoft.Extensions.Hosting;
using System;
using System.Linq;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace LenoPwn.ConfigUI
{
    [SupportedOSPlatform("windows")]
    public class ApplicationHostService : IHostedService
    {
        private MainWindow? _mainWindow;

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await HandleActivationAsync();
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
        }

        private async Task HandleActivationAsync()
        {
            await Task.CompletedTask;

            if (!Application.Current.Windows.OfType<MainWindow>().Any())
            {
                _mainWindow = App.GetService<MainWindow>();
                _mainWindow.Show();
            }
        }
    }
}