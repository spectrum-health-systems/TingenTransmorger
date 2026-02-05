// 260204_code
// 260204_documentation

using System.Windows;
using TingenTransmorger.Core;
using TingenTransmorger.Database;

namespace TingenTransmorger;

/// <summary>Entry class for Tingen Transmorger.</summary>
public partial class MainWindow : Window
{
    /// <summary>Entry method for Tingen Transmorger.</summary>
    public MainWindow()
    {
        InitializeComponent();

        StartApp();
    }

    /// <summary>Performs application startup tasks.</summary>
    private static void StartApp()
    {
        var config = Configuration.Load();

        Framework.Verify(config);

        // This is just during development.
        // Comment out if you don't want to rebuild all reports on each startup.
        //TeleHealthReport.ReportProcessor.Process(config.AdminDirectories["Import"], config.AdminDirectories["Tmp"]);
        TransmorgerDatabase.Build(config.AdminDirectories["Tmp"]);
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
}