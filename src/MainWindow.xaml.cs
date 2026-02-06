// 260206_code
// 260206_documentation

using System.IO;
using System.Windows;
using TingenTransmorger.Core;
using TingenTransmorger.Database;

namespace TingenTransmorger;

/// <summary>Entry class for Tingen Transmorger.</summary>
public partial class MainWindow : Window
{
    public TransmorgerDatabase TransMorgDb { get; set; }

    /// <summary>Entry method for Tingen Transmorger.</summary>
    public MainWindow()
    {
        InitializeComponent();

        StartApp();
    }




    /// <summary>Performs application startup tasks.</summary>
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

    /// <summary>Stops the Tingen Muno application.</summary>
    /// <remarks>
    ///     If you pass a message to <paramref name="msgExit"/>, it will be displayed to the user in a MessageBox before
    ///     the application exits.<br/>
    ///     <br/>
    ///     This method is public because it is called from other methods outside the <see cref="MainWindow"/> class.
    /// </remarks>
    /// <param name="msgExit">An optional exit message to display to the user.</param>
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