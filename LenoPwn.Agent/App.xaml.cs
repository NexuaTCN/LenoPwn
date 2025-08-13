using System;
using System.Threading;
using System.Windows;

namespace LenoPwn.Agent
{
    public partial class App : Application
    {
        private readonly Agent _agent = new();
        private readonly CancellationTokenSource _cts = new();

        protected override async void OnStartup(StartupEventArgs e)
        {
            try
            {
                base.OnStartup(e);

                this.DispatcherUnhandledException += (sender, args) =>
                {
                    args.Handled = true;
                };

                await _agent.Run(_cts.Token);
                Shutdown();
            }
            catch (Exception)
            {
                Shutdown(1);
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            try
            {
                if (Agent._notificationWindow.IsValueCreated)
                {
                    Agent._notificationWindow.Value.Close();
                }

                _cts.Cancel();
                _cts.Dispose();
            }
            catch (Exception)
            {

            }
            finally
            {
                base.OnExit(e);
            }
        }
    }
}