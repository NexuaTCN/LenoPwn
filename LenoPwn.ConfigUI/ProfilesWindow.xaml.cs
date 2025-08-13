using System.Collections.ObjectModel;
using System.Runtime.Versioning;
using System.Windows;
using Wpf.Ui.Controls;

namespace LenoPwn.ConfigUI
{
    [SupportedOSPlatform("windows")]
    public partial class ProfilesWindow : FluentWindow
    {
        private readonly AppConfig _config;
        public ObservableCollection<UserProfile> Profiles { get; }

        public ProfilesWindow(AppConfig config)
        {
            InitializeComponent();
            _config = config;
            Profiles = new ObservableCollection<UserProfile>(_config.Profiles);
            DgvProfiles.ItemsSource = Profiles;

            if (Profiles.Count == 0)
            {
                TxtNoProfiles.Visibility = Visibility.Visible;
                DgvProfiles.Visibility = Visibility.Collapsed;
            }
        }

        private async void BtnLoad_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement { DataContext: UserProfile profile }) return;

            var msgBox = new Wpf.Ui.Controls.MessageBox
            {
                Title = "Confirm Load",
                Content = $"Are you sure you want to load the '{profile.Name}' profile? This will overwrite your current hotkey configuration.",
                PrimaryButtonText = "Yes",
                CloseButtonText = "No"
            };

            var result = await msgBox.ShowDialogAsync();
            if (result != Wpf.Ui.Controls.MessageBoxResult.Primary) return;

            _config.Mappings = new System.Collections.Generic.List<HotkeyMapping>(profile.Config.Mappings);
            ConfigManager.SaveConfig(_config);
            this.DialogResult = true;
            this.Close();
        }

        private async void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement { DataContext: UserProfile profile }) return;

            var msgBox = new Wpf.Ui.Controls.MessageBox
            {
                Title = "Confirm Deletion",
                Content = $"Are you sure you want to delete the '{profile.Name}' profile?",
                PrimaryButtonText = "Yes",
                CloseButtonText = "No"
            };

            var result = await msgBox.ShowDialogAsync();
            if (result != Wpf.Ui.Controls.MessageBoxResult.Primary) return;

            Profiles.Remove(profile);
            _config.Profiles.Remove(profile);
            ConfigManager.SaveConfig(_config);
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            var nameWindow = new ProfileNameWindow { Owner = this };
            if (nameWindow.ShowDialog() != true) return;

            var newProfile = new UserProfile
            {
                Name = nameWindow.ProfileName,
                Config = new AppConfig
                {
                    Mappings = new System.Collections.Generic.List<HotkeyMapping>(_config.Mappings)
                }
            };

            Profiles.Add(newProfile);
            _config.Profiles.Add(newProfile);
            ConfigManager.SaveConfig(_config);

            if (TxtNoProfiles.Visibility == Visibility.Visible)
            {
                TxtNoProfiles.Visibility = Visibility.Collapsed;
                DgvProfiles.Visibility = Visibility.Visible;
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}