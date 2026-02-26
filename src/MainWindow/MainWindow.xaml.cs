// 260226_code
// 260226_documentation

using System.IO;
using System.Windows;
using TingenTransmorger.Core;
//using TingenTransmorger.Database; // Not included (for now) so it's clear when using the Database namespace

namespace TingenTransmorger;
/// <summary>Entry class for Tingen Transmorger.</summary>
/// <remarks>
///   The MainWindow class contains the following partial classes:
///   <list type="bullet">
///     <item>
///       <term>MainWindow.asmx</term>
///       <description>MainWindow XAML markup</description>
///     </item>
///     <item>
///       <term>MainWindow.asmx.cs</term>
///       <description>StartApp/StopApp logic and event handlers</description>
///     </item>
///     <item>
///       <term>MainWindow.AdminMode.cs</term>
///       <description>Logic related to the admin mode</description>
///     </item>
///     <item>
///       <term>MainWindow.DataCopy.cs</term>
///       <description>Details that are copied to the clipboard</description>
///     </item>
///     <item>
///       <term>MainWindow.Details.cs</term>
///       <description>Logic for displaying information</description>
///     </item>
///     <item>
///       <term>MainWindow.MeetingDetails.cs</term>
///       <description>Logic for displaying meeting information</description>
///     </item>
///     <item>
///       <term>MainWindow.PatientDetails.cs</term>
///       <description>Logic for displaying patient information</description>
///     </item>
///     <item>
///       <term>MainWindow.ProviderDetails.cs</term>
///       <description>Logic for displaying provider information</description>
///     </item>
///     <item>
///       <term>MainWindow.UserInterface.cs</term>
///       <description>Logic specific to the user interface.</description>
///     </item>
///   </list>
///   All of these partial classes are located in MainWindow/, but are part of the <c>TingenTransmorger</c> namespace,<br/>
///   not TingenTransmorger.MainWindow, to help keep things clear and organized.
/// </remarks>
public partial class MainWindow : Window
{
    /// <summary>The Transmorger database.</summary>
    /// <remarks>Defined here so it can be used throughout the application.</remarks>
    public Database.TransmorgerDatabase TmDb { get; set; }

    /// <summary>Entry method for Tingen Transmorger.</summary>
    public MainWindow()
    {
        InitializeComponent();

        /* Call StartApp() asynchronously.
         */
        _ = StartApp();
    }

    /// <summary>Starts the application.</summary>
    private async Task StartApp()
    {
        // TODO: Make sure this is verified properly.
        var config = Configuration.Load();

        // TODO: Verify this is working. If the config file doesn't have an Import path, the app crashes.
        Framework.Verify(config);

        /* If the mode is set to Admin, let's do some admin stuff before we load the database.
         */
        if (string.Equals(config.Mode.Trim(), "admin", StringComparison.OrdinalIgnoreCase))
        {
            var flowControl = await EnterAdminMode(config.AdminDirectories["Import"],
                                                   config.AdminDirectories["Tmp"],
                                                   config.StandardDirectories["MasterDb"]);

            /* If EnterAdminMode returns false, it means the user either failed to authenticate or chose to exit from
             * the admin mode dialog.  In that case, we should stop the app instead of continuing to load the database
             * and show the main UI.
             */
            if (!flowControl)
            {
                return;
            }
        }

        string localDbPath  = Path.Combine(config.StandardDirectories["LocalDb"], "transmorger.db");
        string masterDbPath = Path.Combine(config.StandardDirectories["MasterDb"], "transmorger.db");

        Database.TransmorgerDatabase.Update(localDbPath, masterDbPath);

        // TODO: Make sure that is this fails, the app exits and doesn't continue.
        TmDb = Database.TransmorgerDatabase.Load(localDbPath);

        SetInitialUi();
    }

    /// <summary>Stops the application.</summary>
    /// <remarks>
    ///   <para>
    ///     If you pass a message to <paramref name="msgExit"/>, it will be displayed to the user in a MessageBox
    ///     before the application exits.
    ///   </para>
    ///   <para>
    ///     This method is public because it is called from other methods outside the <see cref="MainWindow"/> class.
    ///   </para>
    /// </remarks>
    /// <param name="msgExit">  An optional exit message to display to the user. </param>
    public static void StopApp(string msgExit = "")
    {
        if (!string.IsNullOrEmpty(msgExit))
        {
            MessageBox.Show(msgExit, "Exiting Tingen Transmorger", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        Environment.Exit(0);
    }

    /* EVENT HANDLERS
     */
    private void btnSearchToggle_Clicked(object? sender, RoutedEventArgs e) => SetSearchToggleUi();
    private void rbtnSearchBy_Checked(object sender, RoutedEventArgs e) => ClearUi();
    private void txbxSearch_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e) => ModifySearchResults();
    private void btnUserPhoneDetail_Clicked(object sender, RoutedEventArgs e) => ShowMessageDetails("phone");
    private void btnUserEmailDetail_Clicked(object sender, RoutedEventArgs e) => ShowMessageDetails("email");
    private void lstbxSearchResults_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e) => DisplayDetails();
    private void dgMeetingResults_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e) => MeetingSelected();
    private void btnCopyGeneralMeetingDetail_Click(object sender, RoutedEventArgs e) => CopyGeneralMeetingDetails();
    private void btnCopyPatientMeetingDetail_Click(object sender, RoutedEventArgs e) => CopyPatientMeetingDetails();
    private void btnCopyProviderMeetingDetail_Click(object sender, RoutedEventArgs e) => CopyProviderMeetingDetails();
}