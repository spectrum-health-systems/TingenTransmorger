// 260206_code
// 260206_documentation

using System.IO;
using System.Windows;
using TingenTransmorger.Core;
using TingenTransmorger.Database;

namespace TingenTransmorger;

/// <summary>
/// Entry class for Tingen Transmorger.
/// </summary>
public partial class MainWindow : Window
{
    /// <summary>
    /// The Transmorger database.
    /// </summary>
    /// <remarks>
    /// Defined here so it can be used throughout the application.
    /// </remarks>
    public TransmorgerDatabase TransMorgDb { get; set; }

    /// <summary>
    /// Entry method for Tingen Transmorger.
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();

        StartApp();
    }

    /// <summary>
    /// Starts the application.
    /// </summary>
    private void StartApp()
    {
        var config = Configuration.Load();

        Framework.Verify(config);

        if (config.Mode.Trim().ToLower() == "admin")
        {
            TeleHealthReport.ReportProcessor.Process(config.AdminDirectories["Import"], config.AdminDirectories["Tmp"]);
            TransmorgerDatabase.Build(config.AdminDirectories["Tmp"], config.StandardDirectories["MasterDb"]);
        }

        var localDbPath = Path.Combine(config.StandardDirectories["LocalDb"], "transmorger.db");

        TransMorgDb = TransmorgerDatabase.Load(localDbPath);

        rbtnByName.IsChecked = true;
    }

    /// <summary>
    /// Stops the application.
    /// </summary>
    /// <param name="msgExit">
    /// An optional exit message to display to the user.
    /// </param>
    /// <remarks>
    /// <para>
    /// If you pass a message to <paramref name="msgExit"/>, it will be displayed to the user in a MessageBox before the
    /// application exits.
    /// </para>
    /// <para>This method is public because it is called from other methods outside the <see cref="MainWindow"/> class.</para>
    /// </remarks>
    public static void StopApp(string msgExit = "")
    {
        if (!string.IsNullOrEmpty(msgExit))
        {
            MessageBox.Show(msgExit, "Exiting Tingen Transmorger", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        Environment.Exit(0);
    }

    /*
     * EVENTS
     */

    /// <summary>The search toggle button was clicked.</summary>
    /// <remarks>
    /// <para>
    /// The search toggle button cycles through the three search modes:
    /// - Patient Search
    /// - Provider Search
    /// - Meeting Search.
    /// </para>
    /// <para>This method handles the click event for the search toggle button and updates the button's content accordingly.</para>
    /// </remarks>
    private void SearchToggleClicked()
    {
        switch (btnSearchToggle.Content)
        {
            case "Patient Search":
                btnSearchToggle.Content = "Provider Search";
                break;

            case "Provider Search":
                btnSearchToggle.Content = "Meeting Search";
                break;

            case "Meeting Search":
                btnSearchToggle.Content = "Patient Search";
                break;
        }
    }

    /*
     * EVENT HANDLERS
     */
    private void btnSearchToggle_Click(object? sender, RoutedEventArgs e) => SearchToggleClicked();
}