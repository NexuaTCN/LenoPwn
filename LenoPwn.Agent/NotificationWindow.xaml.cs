using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace LenoPwn.Agent
{
    public partial class NotificationWindow : Window
    {
        private readonly DispatcherTimer _fadeoutTimer;

        public NotificationWindow()
        {
            InitializeComponent();

            _fadeoutTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            _fadeoutTimer.Tick += (sender, args) =>
            {
                _fadeoutTimer.Stop();
                FadeOut();
            };
        }

        private string GetInstallDirectory()
        {
            string exePath = Assembly.GetExecutingAssembly().Location;
            return Path.GetDirectoryName(exePath) ?? AppDomain.CurrentDomain.BaseDirectory;
        }

        private void LoadBackgroundImage(string theme)
        {
            try
            {
                var installDir = GetInstallDirectory();
                var backgroundPath = Path.Combine(installDir, "Icons", $"background_{theme.ToLower()}.png");

                if (File.Exists(backgroundPath))
                {
                    BackgroundImage.Source = new BitmapImage(new Uri(backgroundPath));
                }
                else
                {

                }
            }
            catch (Exception)
            {

            }
        }

        public void ShowNotification(string iconName, string theme)
        {
            if (string.IsNullOrWhiteSpace(theme) || (theme.ToLower() != "light" && theme.ToLower() != "dark"))
            {
                theme = "dark";
            }

            LoadBackgroundImage(theme);

            try
            {
                var installDir = GetInstallDirectory();
                var iconPath = Path.Combine(installDir, "Icons", $"{iconName}_{theme.ToLower()}.png");

                if (File.Exists(iconPath))
                {
                    IconImage.Source = new BitmapImage(new Uri(iconPath));
                }
                else
                {
                    IconImage.Source = null;
                }
            }
            catch (Exception)
            {

            }

            var desktopWorkingArea = SystemParameters.WorkArea;
            this.Left = (desktopWorkingArea.Right / 2) - (this.Width / 2);
            this.Top = desktopWorkingArea.Bottom - this.Height - 20;

            this.Show();
            FadeIn();

            _fadeoutTimer.Stop();
            _fadeoutTimer.Start();
        }

        private void FadeIn()
        {
            var fadeInAnimation = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200));
            RootGrid.BeginAnimation(UIElement.OpacityProperty, fadeInAnimation);
        }

        private void FadeOut()
        {
            var fadeOutAnimation = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(500));
            fadeOutAnimation.Completed += (s, e) => this.Hide();
            RootGrid.BeginAnimation(UIElement.OpacityProperty, fadeOutAnimation);
        }
    }
}