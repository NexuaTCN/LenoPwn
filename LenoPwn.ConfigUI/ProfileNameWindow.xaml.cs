using System.Runtime.Versioning;
using System.Windows;
using System.Windows.Input;
using Wpf.Ui.Controls;

namespace LenoPwn.ConfigUI
{
    [SupportedOSPlatform("windows")]
    public partial class ProfileNameWindow : FluentWindow
    {
        public string ProfileName => TxtProfileName.Text;

        public ProfileNameWindow()
        {
            InitializeComponent();
            this.Loaded += (s, e) => TxtProfileName.Focus();
        }

        private async void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ProfileName))
            {
                var msgBox = new Wpf.Ui.Controls.MessageBox
                {
                    Title = "Invalid Name",
                    Content = "Profile name cannot be empty.",
                    CloseButtonText = "OK"
                };
                await msgBox.ShowDialogAsync();
                return;
            }
            this.DialogResult = true;
            this.Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void TxtProfileName_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                BtnOk_Click(sender, e);
            }
        }
    }
}