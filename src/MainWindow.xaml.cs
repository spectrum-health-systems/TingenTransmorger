// 260205_code
// 260205_documentation

using System.IO;
using System.Windows;
using TingenTransmorger.Core;
using TingenTransmorger.Database;

namespace TingenTransmorger;

/// <summary>Entry class for Tingen Transmorger.</summary>
public partial class MainWindow : Window
{
    public TransmorgerDatabase TransMorgDb { get; set; }
    private enum SearchMode { Patient, Provider, Meeting }
    private SearchMode _searchMode = SearchMode.Patient;

    /// <summary>Entry method for Tingen Transmorger.</summary>
    public MainWindow()
    {
        InitializeComponent();
        StartApp();

        // Wire search toggle button
        btnSearchToggle.Click += BtnSearchToggle_Click;
    }

    private void BtnSearchToggle_Click(object? sender, RoutedEventArgs e)
    {
        // Cycle through Patient -> Provider -> Meeting
        _searchMode = _searchMode switch
        {
            SearchMode.Patient => SearchMode.Provider,
            SearchMode.Provider => SearchMode.Meeting,
            SearchMode.Meeting => SearchMode.Patient,
            _ => SearchMode.Patient
        };

        // Update button text
        btnSearchToggle.Content = _searchMode switch
        {
            SearchMode.Patient => "Patient Search",
            SearchMode.Provider => "Provider Search",
            SearchMode.Meeting => "Meeting Search",
            _ => "Patient Search"
        };
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

        var test =0;
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