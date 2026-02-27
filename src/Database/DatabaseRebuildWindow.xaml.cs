// 260212_code
// 260212_documentation
//

using System.Windows;

namespace TingenTransmorger.Database;
/// <summary>Provide the user with real-time feedback on the progress of the database rebuild process.</summary>
public partial class DatabaseRebuildWindow : Window
{
    /// <summary>Used to track the parent window.</summary>
    private Window? _parentWindow;

    /// <summary>Entry point for the DatabaseRebuildWindow.</summary>
    public DatabaseRebuildWindow()
    {
        InitializeComponent();
    }

    /// <summary>Sets the parent window that should be shown when this window closes.</summary>
    /// <param name="parentWindow">The parent window.</param>
    public void SetParentWindow(Window parentWindow) => _parentWindow = parentWindow;

    /// <summary>Inform the user what the current database rebuild task is.</summary>
    /// <param name="rebuildTask">Database rebuild current task.</param>
    public void UpdateTask(string rebuildTask) => Dispatcher.Invoke(() => txbkCurrentTask.Text = rebuildTask);

    /// <summary>Update the user where they are in the rebuild progress.</summary>
    /// <param name="percentage">Database rebuild progress.</param>
    public void UpdateProgress(double percentage) => Dispatcher.Invoke(() => pbarProgress.Value = percentage);

    /// <summary>Provide the user with a detailed database rebuild status message.</summary>
    /// <param name="rebuildStatus">Detailed database rebuild status message.</param>
    public void UpdateStatus(string rebuildStatus) => Dispatcher.Invoke(() => txbkStatus.Text = rebuildStatus);

    /// <summary>Marks the rebuild as complete and closes the window.</summary>
    public void Complete() => Dispatcher.Invoke(() =>
    {
        txbkCurrentTask.Text = "Database rebuild complete!";
        txbkStatus.Text      = "You can now close this window.";
        pbarProgress.Value   = 100;
        btnClose.Visibility  = Visibility.Visible;
    });

    /// <summary>Closes the DatabaseRebuildWindow and shows the original parent window.</summary>
    private void CloseClick()
    {
        _parentWindow?.Show();

        Close();
    }

    /* EVENT HANDLERS */

    private void btnClose_Click(object sender, RoutedEventArgs e) => CloseClick();
}