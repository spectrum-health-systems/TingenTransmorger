// 260212_code
// 260311_documentation

/* The database namespace needs to be refactored */

using System.Windows;

namespace TingenTransmorger.Database;

public partial class DatabaseRebuildWindow : Window
{

    private Window? _parentWindow;

    public DatabaseRebuildWindow()
    {
        InitializeComponent();
    }

    public void SetParentWindow(Window parentWindow) => _parentWindow = parentWindow;

    public void UpdateTask(string rebuildTask) => Dispatcher.Invoke(() => txbkCurrentTask.Text = rebuildTask);

    public void UpdateProgress(double percentage) => Dispatcher.Invoke(() => pbarProgress.Value = percentage);

    public void UpdateStatus(string rebuildStatus) => Dispatcher.Invoke(() => txbkStatus.Text = rebuildStatus);

    public void Complete() => Dispatcher.Invoke(() =>
    {
        txbkCurrentTask.Text = "Database rebuild complete!";
        txbkStatus.Text      = "You can now close this window.";
        pbarProgress.Value   = 100;
        btnClose.Visibility  = Visibility.Visible;
    });

    private void CloseClick()
    {
        _parentWindow?.Show();

        Close();
    }

    /* EVENT HANDLERS */

    private void btnClose_Click(object sender, RoutedEventArgs e) => CloseClick();
}