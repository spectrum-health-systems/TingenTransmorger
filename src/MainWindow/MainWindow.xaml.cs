// 260218_code
// 260218_documentation

using System.IO;
using System.Text.Json;
using System.Windows;
using TingenTransmorger.Core;
/* I'm choosing to not include this for now, so it's clear when we're using something from the Database namespace.
 */
//using TingenTransmorger.Database;
using TingenTransmorger.Models;

/* I've moved the MainWindow partial classes to MainWindow/ to keep the code organized, but I'm leaving the namespace as
 * TingenTransmorger instead of TingenTransmorger.MainWindow to avoid confusion with the MainWindow class.
 */
namespace TingenTransmorger;

/// <summary>Entry class for Tingen Transmorger.</summary>
/// <remarks>
///  The MainWindow class contains the following partial classes:
///     <list type="bullet">
///     <item>
///         <term>MainWindow.asmx</term>
///         <description>XAML markup</description></item>
///    <item>
///         <term>MainWindow.asmx.cs</term>
///         <description>General logic</description></item>
///    <item>
///         <term>MainWindow.AdminMode.cs</term>
///         <description>Admin mode logic</description></item>
///    <item>
///         <term>MainWindow.DetailDisplay.cs</term>
///         <description>Detail display logic</description></item>
///    <item>
///         <term>MainWindow.Events.cs</term>
///         <description>Event handlers and event logic</description></item>
///    <item>
///         <term>MainWindow.UserInterface.cs</term>
///         <description>User interface logic</description></item>
///   </list>
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

    /* --- */

    /// <summary>Handles the selection changed event for the meetings DataGrid.</summary>
    /// <remarks>
    /// <para>
    /// This method is called when the user selects a meeting from the meetings table.
    /// It retrieves the full meeting details and displays them in the details section.
    /// </para>
    /// </remarks>
    private void MeetingSelected()
    {
        // Don't process selection if database is not yet initialized
        if (TmDb == null)
        {
            spnlMeetingDetailsComponents.Visibility = Visibility.Collapsed;
            return;
        }

        // Get the selected meeting
        var selectedMeeting = dgrdMeetingResults.SelectedItem as PatientMeetingRow;
        if (selectedMeeting == null || string.IsNullOrWhiteSpace(selectedMeeting.MeetingId))
        {
            spnlMeetingDetailsComponents.Visibility = Visibility.Collapsed;
            return;
        }

        // Get meeting details from database
        var meetingDetail = TmDb.GetMeetingDetail(selectedMeeting.MeetingId);
        if (meetingDetail == null)
        {
            spnlMeetingDetailsComponents.Visibility = Visibility.Collapsed;
            return;
        }

        // Helper function to replace null values
        string ReplaceNull(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "---";

            var result = System.Text.RegularExpressions.Regex.Replace(
                value,
                @"\bnull\b",
                "<<NULL>>",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            if (result.Contains("<<NULL>>"))
            {
                var cleanedResult = result.Replace("<<NULL>>", "").Trim().Trim(',').Trim(';').Trim();
                if (string.IsNullOrWhiteSpace(cleanedResult))
                {
                    return "---";
                }

                result = result.Replace("<<NULL>>", "---");
            }

            return string.IsNullOrWhiteSpace(result) ? "---" : result;
        }

        // Extract meeting detail properties
        // Use MeetingId directly from selectedMeeting since we already have it
        var meetingId = selectedMeeting.MeetingId;
        var status = meetingDetail.Value.TryGetProperty("Status", out var statusElem)
            ? (statusElem.GetString() ?? string.Empty) : string.Empty;
        var initiatedBy = meetingDetail.Value.TryGetProperty("InitiatedBy", out var initiatedByElem)
            ? (initiatedByElem.GetString() ?? string.Empty) : string.Empty;
        var scheduledStart = meetingDetail.Value.TryGetProperty("ScheduledStart", out var scheduledStartElem)
            ? (scheduledStartElem.GetString() ?? string.Empty) : string.Empty;
        var actualStart = meetingDetail.Value.TryGetProperty("ActualStart", out var actualStartElem)
            ? (actualStartElem.GetString() ?? string.Empty) : string.Empty;
        var scheduledEnd = meetingDetail.Value.TryGetProperty("ScheduledEnd", out var scheduledEndElem)
            ? (scheduledEndElem.GetString() ?? string.Empty) : string.Empty;
        var actualEnd = meetingDetail.Value.TryGetProperty("ActualEnd", out var actualEndElem)
            ? (actualEndElem.GetString() ?? string.Empty) : string.Empty;
        var endedBy = meetingDetail.Value.TryGetProperty("EndedBy", out var endedByElem)
            ? (endedByElem.GetString() ?? string.Empty) : string.Empty;
        var joins = meetingDetail.Value.TryGetProperty("Joins", out var joinsElem)
            ? (joinsElem.GetString() ?? string.Empty) : string.Empty;
        var duration = meetingDetail.Value.TryGetProperty("Duration", out var durationElem)
            ? (durationElem.GetString() ?? string.Empty) : string.Empty;
        var meetingTitle = meetingDetail.Value.TryGetProperty("MeetingTitle", out var meetingTitleElem)
            ? (meetingTitleElem.GetString() ?? string.Empty) : string.Empty;
        var serviceCode = meetingDetail.Value.TryGetProperty("ServiceCode", out var serviceCodeElem)
            ? (serviceCodeElem.GetString() ?? string.Empty) : string.Empty;


        // Extract workflow, program, and front desk check-in
        var workflow = meetingDetail.Value.TryGetProperty("Workflow", out var workflowElem)
            ? (workflowElem.GetString() ?? string.Empty) : string.Empty;
        var program = meetingDetail.Value.TryGetProperty("Program", out var programElem)
            ? (programElem.GetString() ?? string.Empty) : string.Empty;
        var checkedInByFrontDesk = meetingDetail.Value.TryGetProperty("CheckedInByFrontDesk", out var checkedInElem)
            ? (checkedInElem.GetString() ?? string.Empty) : string.Empty;

        // Populate labels with null-safe values
        txbkMeetingIdValue.Text = ReplaceNull(meetingId ?? string.Empty);
        txbkMeetingStatusValue.Text = ReplaceNull(status ?? string.Empty);
        txbkMeetingTitleValue.Text = ReplaceNull(meetingTitle ?? string.Empty);

        // Populate meeting detail TextBlocks
        txbkMeetingStartedByValue.Text = ReplaceNull(initiatedBy ?? string.Empty);
        txbkMeetingScheduledStartValue.Text = ReplaceNull(scheduledStart ?? string.Empty);
        txbkMeetingActualStartValue.Text = ReplaceNull(actualStart ?? string.Empty);
        txbkMeetingScheduledEndValue.Text = ReplaceNull(scheduledEnd ?? string.Empty);
        txbkMeetingActualEndValue.Text = ReplaceNull(actualEnd ?? string.Empty);
        txbkMeetingEndedByValue.Text = ReplaceNull(endedBy ?? string.Empty);
        txbkMeetingJoins.Text = ReplaceNull(joins ?? string.Empty);
        txbkMeetingDurationValue.Text = ReplaceNull(duration ?? string.Empty);
        txbkMeetingServiceCodeValue.Text = ReplaceNull(serviceCode ?? string.Empty);

        // Populate additional information TextBlocks
        txbkMeetingWorkflowValue.Text = ReplaceNull(workflow ?? string.Empty);
        txbkMeetingProgram.Text = ReplaceNull(program ?? string.Empty);
        txbkMeetingCheckedInByFrontDeskValue.Text = ReplaceNull(checkedInByFrontDesk ?? string.Empty);

        // Get and display meeting error if it exists
        var meetingError = TmDb.GetMeetingError(selectedMeeting.MeetingId);
        if (meetingError != null)
        {
            var kind = meetingError.Value.TryGetProperty("Kind", out var kindElem)
                ? (kindElem.GetString() ?? string.Empty) : string.Empty;
            var reason = meetingError.Value.TryGetProperty("Reason", out var reasonElem)
                ? (reasonElem.GetString() ?? string.Empty) : string.Empty;

            if (!string.IsNullOrWhiteSpace(kind) || !string.IsNullOrWhiteSpace(reason))
            {
                txbkMeetingErrorValue.Text = $"{kind}\n{reason}";
            }
            else
            {
                txbkMeetingErrorValue.Text = "---";
            }
        }
        else
        {
            txbkMeetingErrorValue.Text = "---";
        }

        // Get and display participant meeting quality data from Patients.Meetings
        var qualityData = string.Empty;
        var arrived = string.Empty;
        var dropped = string.Empty;
        var patientDuration = string.Empty;
        var rating = string.Empty;
        var checkInViaChat = string.Empty;
        var checkInWait = string.Empty;
        var waitForCareTeam = string.Empty;
        var waitForProvider = string.Empty;
        var checkOutWait = string.Empty;
        var device = string.Empty;
        var os = string.Empty;
        var browser = string.Empty;

        // Retrieve the patient details to access the meetings array
        var patientDetailsForQuality = TmDb.GetPatientDetails(_currentPatientName, _currentPatientId);

        if (patientDetailsForQuality != null && patientDetailsForQuality.Value.TryGetProperty("Meetings", out var meetingsArray))
        {
            if (meetingsArray.ValueKind == JsonValueKind.Array)
            {
                foreach (var meeting in meetingsArray.EnumerateArray())
                {
                    var mtgId = meeting.TryGetProperty("MeetingId", out var mtgIdElem)
                        ? mtgIdElem.GetString() : null;

                    if (mtgId == selectedMeeting.MeetingId)
                    {
                        // Get all the patient meeting data
                        qualityData = meeting.TryGetProperty("QualityData", out var qualityDataElem)
                            ? (qualityDataElem.GetString() ?? string.Empty) : string.Empty;
                        arrived = meeting.TryGetProperty("Arrived", out var arrivedElem)
                            ? (arrivedElem.GetString() ?? string.Empty) : string.Empty;
                        dropped = meeting.TryGetProperty("Dropped", out var droppedElem)
                            ? (droppedElem.GetString() ?? string.Empty) : string.Empty;
                        patientDuration = meeting.TryGetProperty("Duration", out var patientDurationElem)
                            ? (patientDurationElem.GetString() ?? string.Empty) : string.Empty;
                        rating = meeting.TryGetProperty("Rating", out var ratingElem)
                            ? (ratingElem.GetString() ?? string.Empty) : string.Empty;
                        checkInViaChat = meeting.TryGetProperty("CheckInViaChat", out var checkInViaChatElem)
                            ? (checkInViaChatElem.GetString() ?? string.Empty) : string.Empty;
                        checkInWait = meeting.TryGetProperty("CheckInWait", out var checkInWaitElem)
                            ? (checkInWaitElem.GetString() ?? string.Empty) : string.Empty;
                        waitForCareTeam = meeting.TryGetProperty("WaitForCareTeamMember", out var waitForCareTeamElem)
                            ? (waitForCareTeamElem.GetString() ?? string.Empty) : string.Empty;
                        waitForProvider = meeting.TryGetProperty("WaitForProvider", out var waitForProviderElem)
                            ? (waitForProviderElem.GetString() ?? string.Empty) : string.Empty;
                        checkOutWait = meeting.TryGetProperty("CheckOutWait", out var checkOutWaitElem)
                            ? (checkOutWaitElem.GetString() ?? string.Empty) : string.Empty;
                        device = meeting.TryGetProperty("Device", out var deviceElem)
                            ? (deviceElem.GetString() ?? string.Empty) : string.Empty;
                        os = meeting.TryGetProperty("Os", out var osElem)
                            ? (osElem.GetString() ?? string.Empty) : string.Empty;
                        browser = meeting.TryGetProperty("Browser", out var browserElem)
                            ? (browserElem.GetString() ?? string.Empty) : string.Empty;
                        break;
                    }
                }
            }
        }

        // Populate patient meeting detail fields
        txbkPatientArrivedValue.Text = ReplaceNull(arrived);
        txbkPatientDroppedValue.Text = ReplaceNull(dropped);
        txbkPatientDurationValue.Text = ReplaceNull(patientDuration);
        txbkPatientRatingValue.Text = ReplaceNull(rating);
        txbkCheckedInViaChatValue.Text = ReplaceNull(checkInViaChat);
        txbkCheckInWaitValue.Text = ReplaceNull(checkInWait);
        txbkWaitForCareTeamValue.Text = ReplaceNull(waitForCareTeam);
        txbkWaitForProviderValue.Text = ReplaceNull(waitForProvider);
        txbkCheckOutWaitValue.Text = ReplaceNull(checkOutWait);
        txbkPatientDeviceValue.Text = ReplaceNull(device);
        txbkPatientOsValue.Text = ReplaceNull(os);
        txbkPatientBrowserValue.Text = ReplaceNull(browser);
        txbkMeetingQualityDataValue.Text = ReplaceNull(qualityData);

        // Show/hide patient-specific and provider-specific meeting details based on current view mode
        // If we're viewing a provider, hide the patient-specific section and show provider section
        if (lblPatientProviderKey.Content?.ToString() == "PROVIDER")
        {
            brdrMeetingDetailsPatientContainer.Visibility = Visibility.Collapsed;
            brdrMeetingDetailsProviderContainer.Visibility = Visibility.Visible;

            // Get and display participant names from MeetingDetail
            var participantNames = string.Empty;
            if (meetingDetail.Value.TryGetProperty("ParticipantNames", out var participantNamesElem))
            {
                participantNames = participantNamesElem.GetString() ?? string.Empty;
            }
            txtProviderParticipantNames.Text = ReplaceNull(participantNames);
        }
        else
        {
            brdrMeetingDetailsPatientContainer.Visibility = Visibility.Visible;
            brdrMeetingDetailsProviderContainer.Visibility = Visibility.Collapsed;
        }

        // Show the meeting details section
        spnlMeetingDetailsComponents.Visibility = Visibility.Visible;
    }

    /// <summary>Updates the btnPhoneDetails button appearance based on SMS failure and delivery records.</summary>
    private void UpdatePhoneDetailsButton()
    {
        bool hasFailures = _smsFailures.Count > 0;
        bool hasDeliveries = _smsDeliveries.Count > 0;

        if (!hasFailures && !hasDeliveries)
        {
            // No records: gray background, disabled
            btnPhoneDetails.Background = System.Windows.Media.Brushes.Gray;
            btnPhoneDetails.IsEnabled = false;
        }
        else if (hasFailures && hasDeliveries)
        {
            // Both: yellow background, enabled
            btnPhoneDetails.Background = System.Windows.Media.Brushes.Yellow;
            btnPhoneDetails.IsEnabled = true;
        }
        else if (hasFailures)
        {
            // Only failures: red background, enabled
            btnPhoneDetails.Background = System.Windows.Media.Brushes.Red;
            btnPhoneDetails.IsEnabled = true;
        }
        else
        {
            // Only deliveries: green background, enabled
            btnPhoneDetails.Background = System.Windows.Media.Brushes.Green;
            btnPhoneDetails.IsEnabled = true;
        }
    }

    /// <summary>Handles the phone details button click event.</summary>
    private void PhoneDetailsClicked()
    {
        var messageHistoryWindow = new Database.MessageHistoryWindow(_smsFailures, _smsDeliveries);
        messageHistoryWindow.Owner = this;
        messageHistoryWindow.ShowDialog();
    }

    /// <summary>Updates the btnEmailDetails button appearance based on email failure and delivery records.</summary>
    private void UpdateEmailDetailsButton()
    {
        bool hasFailures = _emailFailures.Count > 0;
        bool hasDeliveries = _emailDeliveries.Count > 0;

        if (!hasFailures && !hasDeliveries)
        {
            // No records: gray background, disabled
            btnEmailDetails.Background = System.Windows.Media.Brushes.Gray;
            btnEmailDetails.IsEnabled = false;
        }
        else if (hasFailures && hasDeliveries)
        {
            // Both: yellow background, enabled
            btnEmailDetails.Background = System.Windows.Media.Brushes.Yellow;
            btnEmailDetails.IsEnabled = true;
        }
        else if (hasFailures)
        {
            // Only failures: red background, enabled
            btnEmailDetails.Background = System.Windows.Media.Brushes.Red;
            btnEmailDetails.IsEnabled = true;
        }
        else
        {
            // Only deliveries: green background, enabled
            btnEmailDetails.Background = System.Windows.Media.Brushes.Green;
            btnEmailDetails.IsEnabled = true;
        }
    }

    /// <summary>Handles the email details button click event.</summary>
    private void EmailDetailsClicked()
    {
        var emailHistoryWindow = new Database.MessageHistoryWindow(_emailFailures, _emailDeliveries, Database.MessageHistoryType.Email);
        emailHistoryWindow.Owner = this;
        emailHistoryWindow.ShowDialog();
    }

    /// <summary>Handles the copy meeting details general button click event.</summary>
    private void CopyMeetingDetailsGeneralClicked()
    {
        try
        {
            var sb = new System.Text.StringBuilder();

            // Header
            sb.AppendLine("=== MEETING DETAILS (GENERAL) ===");
            sb.AppendLine();

            // Left column
            sb.AppendLine("Meeting ID:       " + txbkMeetingIdValue.Text);
            sb.AppendLine("Title:            " + txbkMeetingTitleValue.Text);
            sb.AppendLine("Status:           " + txbkMeetingStatusValue.Text);
            sb.AppendLine("Joins:            " + txbkMeetingJoins.Text);
            sb.AppendLine("Duration:         " + txbkMeetingDurationValue.Text);
            sb.AppendLine("Service code:     " + txbkMeetingServiceCodeValue.Text);
            sb.AppendLine();

            // Center column
            sb.AppendLine("Started by:       " + txbkMeetingStartedByValue.Text);
            sb.AppendLine("Scheduled start:  " + txbkMeetingScheduledStartValue.Text);
            sb.AppendLine("Actual start:     " + txbkMeetingActualStartValue.Text);
            sb.AppendLine("Ended by:         " + txbkMeetingEndedByValue.Text);
            sb.AppendLine("Scheduled end:    " + txbkMeetingScheduledEndValue.Text);
            sb.AppendLine("Actual end:       " + txbkMeetingActualEndValue.Text);
            sb.AppendLine();

            // Right column
            sb.AppendLine("Workflow:         " + txbkMeetingWorkflowValue.Text);
            sb.AppendLine("Program:          " + txbkMeetingProgram.Text);
            sb.AppendLine("Front Desk Check-In: " + txbkMeetingCheckedInByFrontDeskValue.Text);
            sb.AppendLine();
            sb.AppendLine("Meeting error:");
            sb.AppendLine(txbkMeetingErrorValue.Text);

            Clipboard.SetText(sb.ToString());
            MessageBox.Show(this, "Meeting details (General) copied to clipboard.", "Copied", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Failed to copy meeting details: {ex.Message}", "Copy Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    /// <summary>Handles the copy meeting details patient button click event.</summary>
    private void CopyMeetingDetailsPatientClicked()
    {
        try
        {
            var sb = new System.Text.StringBuilder();

            // Header
            sb.AppendLine("=== MEETING DETAILS (PATIENT) ===");
            sb.AppendLine();

            // Left column
            sb.AppendLine("Patient arrived:  " + txbkPatientArrivedValue.Text);
            sb.AppendLine("Patient dropped:  " + txbkPatientDroppedValue.Text);
            sb.AppendLine("Duration:         " + txbkPatientDurationValue.Text);
            sb.AppendLine("Rating:           " + txbkPatientRatingValue.Text);
            sb.AppendLine();

            // Center column
            sb.AppendLine("Checked-In via chat: " + txbkCheckedInViaChatValue.Text);
            sb.AppendLine("Check-In wait:    " + txbkCheckInWaitValue.Text);
            sb.AppendLine("Wait for Care Team: " + txbkWaitForCareTeamValue.Text);
            sb.AppendLine("Wait for provider: " + txbkWaitForProviderValue.Text);
            sb.AppendLine("Check-out wait:   " + txbkCheckOutWaitValue.Text);
            sb.AppendLine();

            // Right column
            sb.AppendLine("Device:           " + txbkPatientDeviceValue.Text);
            sb.AppendLine("OS:               " + txbkPatientOsValue.Text);
            sb.AppendLine("Browser:          " + txbkPatientBrowserValue.Text);
            sb.AppendLine();

            // Quality data (spanning section)
            sb.AppendLine("Participant Meeting Quality Data:");
            sb.AppendLine(txbkMeetingQualityDataValue.Text);

            Clipboard.SetText(sb.ToString());
            MessageBox.Show(this, "Meeting details (Patient) copied to clipboard.", "Copied", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Failed to copy meeting details: {ex.Message}", "Copy Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    /// <summary>Handles the copy meeting details provider button click event.</summary>
    private void CopyMeetingDetailsProviderClicked()
    {
        try
        {
            var sb = new System.Text.StringBuilder();

            // Header
            sb.AppendLine("=== MEETING DETAILS (PROVIDER) ===");
            sb.AppendLine();

            // Participant names
            sb.AppendLine("Participant Names:");
            sb.AppendLine(txtProviderParticipantNames.Text);

            Clipboard.SetText(sb.ToString());
            MessageBox.Show(this, "Meeting details (Provider) copied to clipboard.", "Copied", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Failed to copy meeting details: {ex.Message}", "Copy Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}