using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Windows;

namespace TestGen
{
    public partial class OutputWindow : Window
    {
        public OutputWindow(string rawCode)
        {
            InitializeComponent();

            var formattedCode = SyntaxFactory
                .ParseCompilationUnit(rawCode)
                .NormalizeWhitespace()
                .ToFullString();

            OutputTextBox.Text = formattedCode;
        }

        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(OutputTextBox.Text);
            MessageBox.Show("Copied to clipboard!", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}