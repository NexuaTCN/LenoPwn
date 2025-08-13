using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using Wpf.Ui.Controls;

namespace LenoPwn.ConfigUI
{
    public partial class SendKeysDetectionWindow : FluentWindow
    {
        public List<string> Modifiers { get; private set; } = new List<string>();
        public string Key { get; private set; } = string.Empty;

        public SendKeysDetectionWindow()
        {
            InitializeComponent();
            this.KeyDown += OnKeyDown;
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = true;

            Modifiers.Clear();
            Key = string.Empty;

            var pressedModifiers = e.KeyboardDevice.Modifiers;
            var pressedKey = e.Key;

            if (pressedKey == System.Windows.Input.Key.LeftCtrl || pressedKey == System.Windows.Input.Key.RightCtrl) pressedKey = System.Windows.Input.Key.None;
            if (pressedKey == System.Windows.Input.Key.LeftAlt || pressedKey == System.Windows.Input.Key.RightAlt) pressedKey = System.Windows.Input.Key.None;
            if (pressedKey == System.Windows.Input.Key.LeftShift || pressedKey == System.Windows.Input.Key.RightShift) pressedKey = System.Windows.Input.Key.None;
            if (pressedKey == System.Windows.Input.Key.LWin || pressedKey == System.Windows.Input.Key.RWin) pressedKey = System.Windows.Input.Key.None;
            if (pressedKey == System.Windows.Input.Key.System) pressedKey = e.SystemKey;

            if (pressedKey == System.Windows.Input.Key.None)
            {
                TxtKeyCombo.Text = "Now Hold down another modifier key, or press a non-modifier key.";
                return;
            }

            if ((pressedModifiers & ModifierKeys.Control) == ModifierKeys.Control) Modifiers.Add("control");
            if ((pressedModifiers & ModifierKeys.Alt) == ModifierKeys.Alt) Modifiers.Add("alt");
            if ((pressedModifiers & ModifierKeys.Shift) == ModifierKeys.Shift) Modifiers.Add("shift");
            if ((pressedModifiers & ModifierKeys.Windows) == ModifierKeys.Windows) Modifiers.Add("win");

            Key = GetKeyName(pressedKey);

            TxtKeyCombo.Text = FormatKeyCombo(Modifiers, Key);
        }

        private string GetKeyName(System.Windows.Input.Key key)
        {
            var keyName = key.ToString();
            if (key >= System.Windows.Input.Key.D0 && key <= System.Windows.Input.Key.D9)
            {
                return keyName.Replace("D", "");
            }
            return keyName;
        }

        private string FormatKeyCombo(List<string> modifiers, string key)
        {
            var parts = new List<string>();
            if (modifiers.Contains("control")) parts.Add("Ctrl");
            if (modifiers.Contains("alt")) parts.Add("Alt");
            if (modifiers.Contains("shift")) parts.Add("Shift");
            if (modifiers.Contains("win")) parts.Add("Win");

            parts.Add(key);

            return string.Join(" + ", parts);
        }

        private void BtnAccept_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(Key))
            {
                System.Windows.MessageBox.Show(this, "Please press a valid key combination first.", "No Key Detected", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }
            this.DialogResult = true;
            this.Close();
        }

        private void BtnCancel_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}