using System.Windows;

namespace TingenTransmorger.Database;
/// <summary>
/// Interaction logic for DatabaseRebuild.xaml
/// </summary>
public partial class DatabaseRebuildWindow : Window
{
    private Window? _parentWindow;

    public DatabaseRebuildWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Sets the parent window that should be shown when this window closes.
    /// </summary>
    /// <param name="parentWindow">The parent MainWindow instance</param>
    public void SetParentWindow(Window parentWindow)
    {
        _parentWindow = parentWindow;
    }

    /// <summary>
    /// Updates the current task being processed.
    /// </summary>
    /// <param name="taskName">Name of the current task (e.g., "Processing Visit Stats")</param>
    public void UpdateTask(string taskName)
    {
        Dispatcher.Invoke(() =>
        {
            txtCurrentTask.Text = taskName;
        });
    }

    /// <summary>
    /// Updates the progress percentage.
    /// </summary>
    /// <param name="percentage">Progress percentage (0-100)</param>
    public void UpdateProgress(double percentage)
    {
        Dispatcher.Invoke(() =>
        {
            progressBar.Value = percentage;
        });
    }

    /// <summary>
    /// Updates the detailed status message.
    /// </summary>
    /// <param name="status">Detailed status message</param>
    public void UpdateStatus(string status)
    {
        Dispatcher.Invoke(() =>
        {
            txtStatus.Text = status;
        });
    }

    /// <summary>
    /// Marks the rebuild as complete and closes the window.
    /// </summary>
    public void Complete()
    {
        Dispatcher.Invoke(() =>
        {
            txtCurrentTask.Text = "Complete!";
            txtStatus.Text = "Database rebuild finished successfully.";
            progressBar.Value = 100;

            // Show the close button
            btnClose.Visibility = Visibility.Visible;
        });
    }

    /// <summary>
    /// Handles the Close button click event.
    /// </summary>
    private void btnClose_Click(object sender, RoutedEventArgs e)
    {
        // Show the parent window if set
        if (_parentWindow != null)
        {
            _parentWindow.Show();
        }

        // Close this window
        Close();
    }
}
