using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management;
using System.Runtime.Versioning;
using System.Windows;
using Wpf.Ui.Controls;

namespace LenoPwn.ConfigUI
{
    [SupportedOSPlatform("windows")]
    public partial class DiscoveryWindow : FluentWindow
    {
        private readonly AppConfig _currentConfig;
        private ManagementEventWatcher? _watcher;
        private readonly ObservableCollection<string> _detectedKeysList = new();

        public DiscoveryWindow(AppConfig config)
        {
            InitializeComponent();
            _currentConfig = config;
            LstDetectedKeys.ItemsSource = _detectedKeysList;

            RefreshKeyList();

            this.Loaded += DiscoveryWindow_Loaded;
            this.Closing += DiscoveryWindow_Closing;
        }

        private void DiscoveryWindow_Loaded(object sender, RoutedEventArgs e)
        {
            StartWatching();
        }

        private void DiscoveryWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            _watcher?.Stop();
            _watcher?.Dispose();
        }

        private void StartWatching()
        {
            try
            {
                var eventQuery = new WqlEventQuery("SELECT * FROM LENOVO_UTILITY_EVENT");
                var scope = new ManagementScope(@"\\.\root\WMI");
                _watcher = new ManagementEventWatcher(scope, eventQuery);

                _watcher.EventArrived += OnEventArrived;
                _watcher.Start();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Could not start key listener: {ex.Message}",
                    "Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);

                this.Close();
            }
        }

        private void OnEventArrived(object sender, EventArrivedEventArgs e)
        {
            if (e.NewEvent.GetPropertyValue("PressTypeDataVal") is not uint keyCode) return;

            Application.Current.Dispatcher.Invoke(() =>
            {
                if (_currentConfig.Mappings.Any(m => m.KeyCode == keyCode))
                {
                    return;
                }

                var prompt = new InputWindow("New Key Detected", $"Key with code '{keyCode}' was pressed.\nPlease give it a friendly name (e.g., Mic Mute):");
                prompt.Owner = this;
                if (prompt.ShowDialog() == true && !string.IsNullOrWhiteSpace(prompt.ResponseText))
                {
                    _currentConfig.Mappings.Add(new HotkeyMapping
                    {
                        KeyCode = keyCode,
                        Description = prompt.ResponseText,
                        Action = "Unassigned"
                    });
                    RefreshKeyList();
                }
            });
        }

        private void RefreshKeyList()
        {
            _detectedKeysList.Clear();
            foreach (var mapping in _currentConfig.Mappings.OrderBy(m => m.KeyCode))
            {
                _detectedKeysList.Add($"'{mapping.Description}' (Code: {mapping.KeyCode})");
            }
        }

        private void BtnDone_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}