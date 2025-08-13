using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LenoPwn.Agent
{
    [SupportedOSPlatform("windows")]
    internal class Agent
    {
        private const string PipeName = "LenoPwnPipe";
        internal static readonly Lazy<NotificationWindow> _notificationWindow = new(() => new NotificationWindow());

        public async Task Run(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        var pipeSecurity = new PipeSecurity();
                        var currentUser = WindowsIdentity.GetCurrent().User;

                        if (currentUser != null)
                        {
                            pipeSecurity.AddAccessRule(new PipeAccessRule(
                                currentUser,
                                PipeAccessRights.FullControl, AccessControlType.Allow));
                        }

                        pipeSecurity.AddAccessRule(new PipeAccessRule(
                            new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null),
                            PipeAccessRights.ReadWrite, AccessControlType.Allow));

                        using var pipeServer = NamedPipeServerStreamAcl.Create(
                            PipeName,
                            PipeDirection.In,
                            1,
                            PipeTransmissionMode.Byte,
                            PipeOptions.Asynchronous,
                            0,
                            0,
                            pipeSecurity);

                        await pipeServer.WaitForConnectionAsync(token);

                        using var reader = new StreamReader(pipeServer, Encoding.UTF8, false, 1024, true);
                        while (!token.IsCancellationRequested && pipeServer.IsConnected)
                        {
                            var command = await reader.ReadLineAsync(token);
                            if (string.IsNullOrEmpty(command))
                            {
                                continue;
                            }

                            var parts = command.Split(new[] { "::" }, StringSplitOptions.None);

                            if (parts.Length > 0)
                            {
                                switch (parts[0])
                                {
                                    case "launch":
                                        if (parts.Length > 1)
                                        {
                                            string appToLaunch = parts[1];
                                            try
                                            {
                                                var processStartInfo = new ProcessStartInfo();
                                                int firstSpaceIndex = appToLaunch.IndexOf(' ');

                                                if (firstSpaceIndex > 0)
                                                {
                                                    processStartInfo.FileName = appToLaunch.Substring(0, firstSpaceIndex);
                                                    processStartInfo.Arguments = appToLaunch.Substring(firstSpaceIndex + 1);
                                                }
                                                else
                                                {
                                                    processStartInfo.FileName = appToLaunch;
                                                }

                                                processStartInfo.UseShellExecute = true;
                                                Process.Start(processStartInfo);
                                            }
                                            catch (Exception)
                                            {

                                            }
                                        }
                                        break;

                                    case "show_icon":
                                        if (parts.Length > 2)
                                        {
                                            string iconToShow = parts[1];
                                            string theme = parts[2];
                                            try
                                            {
                                                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                                                {
                                                    _notificationWindow.Value.ShowNotification(iconToShow, theme);
                                                });
                                            }
                                            catch (Exception)
                                            {

                                            }
                                        }
                                        break;

                                    case "sendkeys":
                                        if (parts.Length > 2)
                                        {
                                            var modifiers = parts[1].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                                            string key = parts[2];
                                            try
                                            {
                                                KeystrokeSimulator.Send(modifiers, key);
                                            }
                                            catch (Exception)
                                            {

                                            }
                                        }
                                        break;
                                }
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception)
                    {
                        await Task.Delay(5000, token);
                    }
                }
            }
            catch (Exception)
            {

            }
        }
    }
}