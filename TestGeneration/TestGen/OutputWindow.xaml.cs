using System.Windows;

namespace TestGen
{
    public partial class OutputWindow : Window
    {
        public OutputWindow(string text)
        {
            InitializeComponent();
            OutputTextBox.Text = text;
        }

        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(OutputTextBox.Text);
            MessageBox.Show("Copied to clipboard!", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
