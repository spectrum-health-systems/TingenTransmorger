// 260218_code
// 260218_documentation

using System.IO;
using System.Windows;
using TingenTransmorger.Core;
//using TingenTransmorger.Database; // Not included (for now) so it's clear when using the Database namespace

namespace TingenTransmorger;

/// <summary>Entry class for Tingen Transmorger.</summary>
/// <remarks>
///     The MainWindow class contains the following partial classes:
///     <list type="bullet">
///         <item>
///             <term>MainWindow.asmx</term>
///             <description>XAML markup</description>
///         </item>
///         <item>
///             <term>MainWindow.asmx.cs</term>
///             <description>StartApp/StopApp logic, properties, and event handlers</description>
///         </item>
///         <item>
///             <term>MainWindow.AdminMode.cs</term>
///             <description>Admin mode logic</description>
///         </item>
///         <item>
///             <term>MainWindow.DataCopy.cs</term>
///             <description>Data copy logic</description>
///         </item>
///         <item>
///             <term>MainWindow.DetailDisplay.cs</term>
///             <description>Detail display logic</description>
///         </item>
///         <item>
///             <term>MainWindow.Events.cs</term>
///             <description>Event handlers and event logic</description>
///         </item>
///         <item>
///             <term>MainWindow.UserInterface.cs</term>
///             <description>User interface logic</description>
///         </item>
///     </list>
///     All of these partial classes are located in MainWindow/, and are part of the TingenTransmorger namespace (and<br/>
///     not the TingenTransmorger.MainWindow class). This is to keep the code organized and make it easier to find<br/>
///     specific logic related to the MainWindow class.
/// </remarks>
public partial class MainWindow : Window
{
    /// <summary>The Transmorger database.</summary>
    /// <remarks>Defined here so it can be used throughout the application.</remarks>
    public Database.TransmorgerDatabase TmDb { get; set; }

    /// <summary>SMS failure records for the current patient's phone numbers.</summary>
    private List<(string PhoneNumber, string ErrorMessage, string ScheduledStartTime)> _smsFailures = new();

    /// <summary>Message delivery records for the current patient's phone numbers.</summary>
    private List<(string PhoneNumber, string DeliveryStatus, string MessageType, string ErrorMessage, string DateSent, string TimeSent)> _smsDeliveries = new();

    /// <summary>Email failure records for the current patient's email addresses.</summary>
    private List<(string EmailAddress, string ErrorMessage, string ScheduledStartTime)> _emailFailures = new();

    /// <summary>Email delivery records for the current patient's email addresses.</summary>
    private List<(string EmailAddress, string DeliveryStatus, string MessageType, string ErrorMessage, string DateSent, string TimeSent)> _emailDeliveries = new();

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

        SetupInitialUi();
    }

    /// <summary>Stops the application.</summary>
    /// <remarks>
    ///     <para>
    ///         If you pass a message to <paramref name="msgExit"/>, it will be displayed to the user in a MessageBox
    ///         before the application exits.
    ///     </para>
    ///     <para>
    ///         This method is public because it is called from other methods outside the <see cref="MainWindow"/> class.
    ///     </para>
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
    private void btnSearchToggle_Clicked(object? sender, RoutedEventArgs e)
        => SetSearchToggleContent(btnSearchToggle.Content.ToString());

    private void rbtnSearchBy_Checked(object sender, RoutedEventArgs e)
        => ClearUi();

    private void btnPhoneDetails_Clicked(object sender, RoutedEventArgs e)
        => ShowPhoneDetails();

    private void btnEmailDetails_Clicked(object sender, RoutedEventArgs e)
        => ShowEmailDetails();

    private void dgPatientMeetings_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        => MeetingSelected();

    private void btnCopyMeetingDetailsGeneral_Click(object sender, RoutedEventArgs e)
        => CopyGeneralMeetingDetails();

    private void btnCopyMeetingDetailsPatient_Click(object sender, RoutedEventArgs e)
        => CopyPatientMeetingDetails();

    private void btnCopyMeetingDetailsProvider_Click(object sender, RoutedEventArgs e)
        => CopyProviderMeetingDetails();
}