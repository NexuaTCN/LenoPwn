using System.Windows;
using System.Windows.Input;
using Wpf.Ui.Controls;

namespace LenoPwn.ConfigUI
{
    public partial class InputWindow : FluentWindow
    {
        public string ResponseText => TxtInput.Text;

        public InputWindow(string title, string prompt)
        {
            InitializeComponent();
            this.Title = title;
            LblPrompt.Text = prompt;
            this.Loaded += (s, e) => TxtInput.Focus();
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        private void TxtInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                BtnOk_Click(sender, e);
            }
        }
    }
}