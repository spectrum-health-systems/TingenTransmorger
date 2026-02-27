// 260227_code
// 260227_documentation

using System.IO;
using System.Windows;
using TingenTransmorger.Core;
using TingenTransmorger.Database;

namespace TingenTransmorger;
/// <summary>Entry class for Tingen Transmorger.</summary>
/// <remarks>
/// The MainWindow class is the entry point for the Tingen Transmorger application, and is split into multiple partial
/// classes. Initially this was done to keep the code organized and maintainable, but over time it has
/// become somewhat of a monster. Eventually this class should be refactored to separate classes.<br/>
/// <br/>
/// The MainWindow.xaml.cs partial class is responsible for the main application flow.
/// </remarks>
public partial class MainWindow : Window
{
    /// <summary>The Transmorger database.</summary>
    private TransmorgerDatabase _tmDb;

    /// <summary>Entry method for Tingen Transmorger.</summary>
    public MainWindow()
    {
        InitializeComponent();

        _ = StartApp();
    }

    /// <summary>Stop the application.</summary>
    /// <remarks>
    /// If you pass a message to <paramref name="msgExit"/>, it will be displayed to the user in a MessageBox
    /// before the application exits.<br/>
    /// <br/>
    /// This method is public because it is called from other methods outside the <see cref="MainWindow"/> class.
    /// </remarks>
    /// <param name="msgExit">Optional exit message to display.</param>
    public static void StopApp(string msgExit = "")
    {
        if (!string.IsNullOrEmpty(msgExit))
        {
            MessageBox.Show(msgExit, "Exiting Tingen Transmorger", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        Environment.Exit(0);
    }

    /// <summary>Start the application.</summary>
    private async Task StartApp()
    {
        var config = Configuration.Load();

        Framework.Verify(config);

        /* If the mode is set to Admin...
         */
        if (string.Equals(config.Mode.Trim(), "admin", StringComparison.OrdinalIgnoreCase))
        {
            var flowControl = await EnterAdminMode(config.AdminDirectories["Import"], config.AdminDirectories["Tmp"], config.StandardDirectories["MasterDb"]);

            /* If EnterAdminMode returns false, it means the user either failed to authenticate or chose to exit from
             * the admin mode dialog. In that case, we should stop the app instead of continuing to load the database
             * and show the main UI.
             */
            if (!flowControl)
            {
                return;
            }
        }

        string localDbPath  = Path.Combine(config.StandardDirectories["LocalDb"], "transmorger.db");
        string masterDbPath = Path.Combine(config.StandardDirectories["MasterDb"], "transmorger.db");

        TransmorgerDatabase.Update(localDbPath, masterDbPath);

        try
        {
            _tmDb = TransmorgerDatabase.Load(localDbPath);
        }
        catch (Exception ex)
        {
            StopApp($"The database could not be loaded: {ex.Message}{Environment.NewLine}{Environment.NewLine}The application will now exit.");
        }

        if (_tmDb is null)
        {
            StopApp("The database could not be loaded. The application will now exit.");
        }

        SetInitialUi();
    }

    /* EVENT HANDLERS */
    private void btnSearchToggle_Clicked(object? sender, RoutedEventArgs e) => SetSearchToggleUi();
    private void rbtnSearchBy_Checked(object sender, RoutedEventArgs e) => ClearUi();
    private void txbxSearch_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e) => UpdateSearchResults();
    private void btnUserPhoneDetail_Clicked(object sender, RoutedEventArgs e) => ShowMessageDetails("phone");
    private void btnUserEmailDetail_Clicked(object sender, RoutedEventArgs e) => ShowMessageDetails("email");
    private void lstbxSearchResults_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e) => Display();
    private void dgMeetingResults_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e) => MeetingSelected();
    private void btnCopyGeneralMeetingDetail_Click(object sender, RoutedEventArgs e) => CopyGeneralMeetingDetails();
    private void btnCopyPatientMeetingDetail_Click(object sender, RoutedEventArgs e) => CopyPatientMeetingDetails();
    private void btnCopyProviderMeetingDetail_Click(object sender, RoutedEventArgs e) => CopyProviderMeetingDetails();
}