using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.Versioning;
using System.Windows;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace LenoPwn.ConfigUI
{
    [SupportedOSPlatform("windows")]
    public partial class MainWindow : FluentWindow
    {
        private AppConfig _config;
        public ObservableCollection<HotkeyMapping> HotkeyMappings { get; }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            _config = ConfigManager.LoadConfig();
            HotkeyMappings = new ObservableCollection<HotkeyMapping>(_config.Mappings);
            DgvHotkeys.ItemsSource = HotkeyMappings;

            bool isDarkMode = _config.Theme != "Light";
            ApplicationThemeManager.Apply(isDarkMode ? ApplicationTheme.Dark : ApplicationTheme.Light);
            CmbTheme.SelectedIndex = isDarkMode ? 0 : 1;
        }

        private void CmbTheme_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (CmbTheme is null) return;

            var theme = CmbTheme.SelectedIndex == 0 ? ApplicationTheme.Dark : ApplicationTheme.Light;
            ApplicationThemeManager.Apply(theme);
            _config.Theme = theme == ApplicationTheme.Dark ? "Dark" : "Light";
        }

        private async void BtnSave_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                _config.Mappings = new List<HotkeyMapping>(HotkeyMappings);
                ConfigManager.SaveConfig(_config);

                var msgBox = new Wpf.Ui.Controls.MessageBox
                {
                    Title = "Success",
                    Content = "Configuration saved successfully!",
                    CloseButtonText = "OK"
                };
                await msgBox.ShowDialogAsync();
            }
            catch (Exception ex)
            {
                var msgBox = new Wpf.Ui.Controls.MessageBox
                {
                    Title = "Save Error",
                    Content = $"An error occurred while saving: {ex.Message}",
                    CloseButtonText = "OK"
                };
                await msgBox.ShowDialogAsync();
            }
        }

        private void BtnRediscover_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var discoveryWindow = new DiscoveryWindow(_config);
            discoveryWindow.Owner = this;
            discoveryWindow.ShowDialog();

            RefreshHotkeys();
        }

        private async void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not System.Windows.Controls.Button button || button.DataContext is not HotkeyMapping mappingToDelete) return;

            var msgBox = new Wpf.Ui.Controls.MessageBox
            {
                Title = "Confirm Deletion",
                Content = $"Are you sure you want to delete the hotkey mapping for '{mappingToDelete.Description}'?",
                PrimaryButtonText = "Yes",
                CloseButtonText = "No"
            };

            var result = await msgBox.ShowDialogAsync();

            if (result == Wpf.Ui.Controls.MessageBoxResult.Primary)
            {
                HotkeyMappings.Remove(mappingToDelete);
            }
        }

        private void DetectSendKeys_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not System.Windows.Controls.Button button || button.DataContext is not HotkeyMapping mapping) return;
            if (mapping.Payload is not SendKeysPayload payload) return;

            var detectionWindow = new SendKeysDetectionWindow { Owner = this };

            if (detectionWindow.ShowDialog() == true)
            {
                payload.Modifiers = new List<string>(detectionWindow.Modifiers);
                payload.Key = detectionWindow.Key;
            }
        }

        private void BtnProfiles_Click(object sender, RoutedEventArgs e)
        {
            var profilesWindow = new ProfilesWindow(_config) { Owner = this };
            if (profilesWindow.ShowDialog() == true)
            {
                RefreshHotkeys();
            }
        }

        private void RefreshHotkeys()
        {
            HotkeyMappings.Clear();
            foreach (var mapping in _config.Mappings)
            {
                HotkeyMappings.Add(mapping);
            }
        }
    }
}