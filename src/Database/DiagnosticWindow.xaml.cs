using System.Windows;

namespace TingenTransmorger.Database;

/// <summary>
/// Window to display diagnostic information with copyable text.
/// </summary>
public partial class DiagnosticWindow : Window
{
    public DiagnosticWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Sets the diagnostic text to display.
    /// </summary>
    /// <param name="diagnosticText">The diagnostic information to display.</param>
    public void SetDiagnosticText(string diagnosticText)
    {
        txtDiagnostic.Text = diagnosticText;
    }

    private void btnCopyToClipboard_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(txtDiagnostic.Text))
        {
            Clipboard.SetText(txtDiagnostic.Text);
            MessageBox.Show("Diagnostic text copied to clipboard!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void btnClose_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
