// 260206_code
// 260206_documentation

using System.IO;
using System.Text.Json;
using System.Windows;
using TingenTransmorger.Core;
using TingenTransmorger.Database;
using TingenTransmorger.Models;

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
    /// Currently selected patient name.
    /// </summary>
    private string _currentPatientName = string.Empty;

    /// <summary>
    /// Currently selected patient ID.
    /// </summary>
    private string _currentPatientId = string.Empty;

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
        spnlPatientDetails.Visibility = Visibility.Collapsed;
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

        // Clear search box and results when toggling
        txbxSearch.Text = string.Empty;
        lstbxSearchResults.Items.Clear();

        // Clear and hide details panel
        //txtDetailsPlaceholder.Visibility = Visibility.Visible;
        spnlPatientDetails.Visibility = Visibility.Collapsed;
        spnlPatientMeetings.Visibility = Visibility.Collapsed;
        spnlMeetingDetails.Visibility = Visibility.Collapsed;
    }

    /// <summary>Handles the search text changed event.</summary>
    /// <remarks>
    /// <para>
    /// This method is called when the user types in the search text box.
    /// It filters and displays results based on the current search mode and search type (by name or ID).
    /// </para>
    /// </remarks>
    private void SearchTextChanged()
    {
        // Clear previous results
        lstbxSearchResults.Items.Clear();

        // Only search for patients when in Patient Search mode
        if (btnSearchToggle.Content.ToString() != "Patient Search")
        {
            return;
        }

        var searchText = txbxSearch.Text?.Trim();

        // Don't search if text is empty or too short
        if (string.IsNullOrWhiteSpace(searchText))
        {
            return;
        }

        // Get all patients from the database
        var allPatients = TransMorgDb.GetPatients();

        // Filter patients based on search type
        var filteredPatients = new List<(string PatientName, string PatientId)>();

        if (rbtnByName.IsChecked == true)
        {
            // Search by name - case insensitive
            filteredPatients = allPatients
                .Where(p => p.PatientName.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                .OrderBy(p => p.PatientName)
                .ToList();
        }
        else if (rbtnById.IsChecked == true)
        {
            // Search by ID
            filteredPatients = allPatients
                .Where(p => p.PatientId.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                .OrderBy(p => p.PatientName)
                .ToList();
        }

        // Display results in the format "PatientName (PatientId)"
        foreach (var patient in filteredPatients)
        {
            lstbxSearchResults.Items.Add($"{patient.PatientName} ({patient.PatientId})");
        }
    }

    /// <summary>Handles the selection changed event for the search results list.</summary>
    /// <remarks>
    /// <para>
    /// This method is called when the user selects a patient from the search results.
    /// It retrieves the full patient details and displays them in the details panel.
    /// </para>
    /// </remarks>
    private void SearchResultSelected()
    {
        // Hide placeholder and show patient details section
        //txtDetailsPlaceholder.Visibility = Visibility.Collapsed;
        spnlPatientDetails.Visibility = Visibility.Visible;

        // Get the selected item
        var selectedItem = lstbxSearchResults.SelectedItem as string;
        if (string.IsNullOrWhiteSpace(selectedItem))
        {
            return;
        }

        // Parse the selected item to extract PatientName and PatientId
        // Format is "PatientName (PatientId)"
        var lastParenIndex = selectedItem.LastIndexOf('(');
        if (lastParenIndex == -1)
        {
            return;
        }

        var patientName = selectedItem.Substring(0, lastParenIndex).Trim();
        var patientId = selectedItem.Substring(lastParenIndex + 1).TrimEnd(')').Trim();

        // Store current patient info for use in other methods
        _currentPatientName = patientName;
        _currentPatientId = patientId;

        // Get patient details from database
        var patientDetails = TransMorgDb.GetPatientDetails(patientName, patientId);
        if (patientDetails == null)
        {
            return;
        }

        // Display patient name and ID
        lblPatientNameValue.Content = patientName;
        lblPatientIdValue.Content = patientId;

        // Display phone numbers
        var phoneNumbers = new List<string>();
        if (patientDetails.Value.TryGetProperty("PhoneNumbers", out var phoneNumbersArray))
        {
            if (phoneNumbersArray.ValueKind == JsonValueKind.Array)
            {
                foreach (var phoneEntry in phoneNumbersArray.EnumerateArray())
                {
                    if (phoneEntry.TryGetProperty("Number", out var numberElem))
                    {
                        var number = numberElem.GetString();
                        if (!string.IsNullOrWhiteSpace(number))
                        {
                            phoneNumbers.Add(number);
                        }
                    }
                }
            }
        }

        if (phoneNumbers.Count == 0)
        {
            phoneNumbers.Add("No phone numbers on file");
        }

        lblPatientPhoneValue.Content = string.Join(", ", phoneNumbers);

        // Display email addresses
        var emailAddresses = new List<string>();
        if (patientDetails.Value.TryGetProperty("EmailAddresses", out var emailAddressesArray))
        {
            if (emailAddressesArray.ValueKind == JsonValueKind.Array)
            {
                foreach (var emailEntry in emailAddressesArray.EnumerateArray())
                {
                    if (emailEntry.TryGetProperty("Address", out var addressElem))
                    {
                        var address = addressElem.GetString();
                        if (!string.IsNullOrWhiteSpace(address))
                        {
                            emailAddresses.Add(address);
                        }
                    }
                }
            }
        }

        if (emailAddresses.Count == 0)
        {
            emailAddresses.Add("No email addresses on file");
        }

        lblPatientEmailValue.Content = string.Join(", ", emailAddresses);

        // Display meetings
        var meetingRows = new List<PatientMeetingRow>();
        if (patientDetails.Value.TryGetProperty("Meetings", out var meetingsArray))
        {
            if (meetingsArray.ValueKind == JsonValueKind.Array)
            {
                foreach (var meeting in meetingsArray.EnumerateArray())
                {
                    // Get MeetingId from Patients.Meetings
                    var meetingId = meeting.TryGetProperty("MeetingId", out var meetingIdElem)
                        ? meetingIdElem.GetString() : null;

                    if (string.IsNullOrWhiteSpace(meetingId))
                    {
                        continue;
                    }

                    // Get Arrived, Dropped, Duration from Patients.Meetings
                    var arrived = meeting.TryGetProperty("Arrived", out var arrivedElem)
                        ? arrivedElem.GetString() : string.Empty;
                    var dropped = meeting.TryGetProperty("Dropped", out var droppedElem)
                        ? droppedElem.GetString() : string.Empty;
                    var duration = meeting.TryGetProperty("Duration", out var durationElem)
                        ? (durationElem.GetString() ?? string.Empty) : string.Empty;

                    // Get ScheduledStart and Status from MeetingDetail
                    var meetingDetail = TransMorgDb.GetMeetingDetail(meetingId);
                    var scheduledStart = string.Empty;
                    var status = string.Empty;

                    if (meetingDetail != null)
                    {
                        scheduledStart = meetingDetail.Value.TryGetProperty("ScheduledStart", out var startElem)
                            ? startElem.GetString() : string.Empty;
                        status = meetingDetail.Value.TryGetProperty("Status", out var statusElem)
                            ? statusElem.GetString() : string.Empty;
                    }

                    // Replace any occurrence of "null" (case-insensitive) with a single "---"
                    string ReplaceNull(string value)
                    {
                        if (string.IsNullOrWhiteSpace(value))
                            return "---";

                        // Replace all occurrences of "null" (case-insensitive) with a placeholder
                        var result = System.Text.RegularExpressions.Regex.Replace(
                            value,
                            @"\bnull\b",
                            "<<NULL>>",
                            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                        // Check if we had any replacements
                        if (result.Contains("<<NULL>>"))
                        {
                            // If the entire string is just null markers (with possible whitespace/separators), return single "---"
                            var cleanedResult = result.Replace("<<NULL>>", "").Trim().Trim(',').Trim(';').Trim();
                            if (string.IsNullOrWhiteSpace(cleanedResult))
                            {
                                return "---";
                            }

                            // Otherwise replace all null markers with "---" 
                            result = result.Replace("<<NULL>>", "---");
                        }

                        return string.IsNullOrWhiteSpace(result) ? "---" : result;
                    }

                    // Check if meeting has an error
                    var hasError = TransMorgDb.HasMeetingError(meetingId);

                    // Check status flags (case-insensitive)
                    var statusLower = status?.ToLower() ?? string.Empty;
                    var isCancelled = statusLower.Contains("cancel");
                    var isCompleted = statusLower.Contains("complete");

                    meetingRows.Add(new PatientMeetingRow
                    {
                        MeetingId = meetingId,
                        Start = ReplaceNull(scheduledStart ?? string.Empty),
                        Arrived = ReplaceNull(arrived ?? string.Empty),
                        Dropped = ReplaceNull(dropped ?? string.Empty),
                        Duration = ReplaceNull(duration ?? string.Empty),
                        Status = ReplaceNull(status ?? string.Empty),
                        HasError = hasError,
                        IsCancelled = isCancelled,
                        IsCompleted = isCompleted
                    });
                }
            }
        }

        // Sort meetings by ScheduledStart descending (most recent first)
        meetingRows = meetingRows
            .OrderByDescending(m => m.Start)
            .ToList();

        // Count meetings by status
        var totalCount = meetingRows.Count;
        var completedCount = meetingRows.Count(m => m.IsCompleted);
        var cancelledCount = meetingRows.Count(m => m.IsCancelled);

        // Count In-Progress, Expired, and Scheduled
        var inProgressCount = 0;
        var expiredCount = 0;
        var scheduledCount = 0;

        foreach (var meeting in meetingRows)
        {
            var statusLower = meeting.Status?.ToLower() ?? string.Empty;

            // Skip already counted statuses
            if (meeting.IsCompleted || meeting.IsCancelled)
                continue;

            if (statusLower.Contains("in progress") || statusLower.Contains("in-progress"))
                inProgressCount++;
            else if (statusLower.Contains("expired"))
                expiredCount++;
            else if (statusLower.Contains("scheduled"))
                scheduledCount++;
        }

        // Update the header with the detailed count using individual TextBlocks
        txtMeetingsTotal.Text = $"{totalCount} MEETINGS";
        txtMeetingsCompleted.Text = $"{completedCount} Completed";
        txtMeetingsInProgress.Text = $"{inProgressCount} In-Progress";
        txtMeetingsExpired.Text = $"{expiredCount} Expired";
        txtMeetingsCancelled.Text = $"{cancelledCount} Cancelled";
        txtMeetingsScheduled.Text = $"{scheduledCount} Scheduled";

        // Bind to DataGrid
        dgPatientMeetings.ItemsSource = meetingRows;

        // Show meetings section if there are meetings
        if (meetingRows.Count > 0)
        {
            spnlPatientMeetings.Visibility = Visibility.Visible;
        }
        else
        {
            spnlPatientMeetings.Visibility = Visibility.Collapsed;
        }

        // Hide meeting details until a meeting is selected
        spnlMeetingDetails.Visibility = Visibility.Collapsed;
    }

    /// <summary>Handles the selection changed event for the meetings DataGrid.</summary>
    /// <remarks>
    /// <para>
    /// This method is called when the user selects a meeting from the meetings table.
    /// It retrieves the full meeting details and displays them in the details section.
    /// </para>
    /// </remarks>
    private void MeetingSelected()
    {
        // Get the selected meeting
        var selectedMeeting = dgPatientMeetings.SelectedItem as PatientMeetingRow;
        if (selectedMeeting == null || string.IsNullOrWhiteSpace(selectedMeeting.MeetingId))
        {
            spnlMeetingDetails.Visibility = Visibility.Collapsed;
            return;
        }

        // Get meeting details from database
        var meetingDetail = TransMorgDb.GetMeetingDetail(selectedMeeting.MeetingId);
        if (meetingDetail == null)
        {
            spnlMeetingDetails.Visibility = Visibility.Collapsed;
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

        // Condensed section properties
        var appointmentId = meetingDetail.Value.TryGetProperty("AppointmentId", out var appointmentIdElem)
            ? (appointmentIdElem.GetString() ?? string.Empty) : string.Empty;
        var workflow = meetingDetail.Value.TryGetProperty("Workflow", out var workflowElem)
            ? (workflowElem.GetString() ?? string.Empty) : string.Empty;
        var program = meetingDetail.Value.TryGetProperty("Program", out var programElem)
            ? (programElem.GetString() ?? string.Empty) : string.Empty;
        var checkedInByFrontDesk = meetingDetail.Value.TryGetProperty("CheckedInByFrontDesk", out var checkedInElem)
            ? (checkedInElem.GetString() ?? string.Empty) : string.Empty;
        var scribeEnabled = meetingDetail.Value.TryGetProperty("ScribeEnabled", out var scribeEnabledElem)
            ? (scribeEnabledElem.GetString() ?? string.Empty) : string.Empty;
        var scribeConsentAcceptance = meetingDetail.Value.TryGetProperty("ScribeConsentAcceptance", out var scribeConsentElem)
            ? (scribeConsentElem.GetString() ?? string.Empty) : string.Empty;

        // Populate labels with null-safe values
        lblMeetingIdValue.Text = ReplaceNull(meetingId ?? string.Empty);
        lblMeetingStatusValue.Text = ReplaceNull(status ?? string.Empty);
        lblMeetingTitleValue.Text = ReplaceNull(meetingTitle ?? string.Empty);

        // Populate meeting detail TextBlocks
        lblMeetingInitiatedBy.Text = ReplaceNull(initiatedBy ?? string.Empty);
        lblMeetingScheduledStart.Text = ReplaceNull(scheduledStart ?? string.Empty);
        lblMeetingActualStart.Text = ReplaceNull(actualStart ?? string.Empty);
        lblMeetingScheduledEnd.Text = ReplaceNull(scheduledEnd ?? string.Empty);
        lblMeetingActualEnd.Text = ReplaceNull(actualEnd ?? string.Empty);
        lblMeetingEndedBy.Text = ReplaceNull(endedBy ?? string.Empty);
        lblMeetingJoins.Text = ReplaceNull(joins ?? string.Empty);
        lblMeetingDuration.Text = ReplaceNull(duration ?? string.Empty);
        lblMeetingServiceCode.Text = ReplaceNull(serviceCode ?? string.Empty);

        // Populate additional information TextBlocks
        txtMeetingWorkflow.Text = ReplaceNull(workflow ?? string.Empty);
        txtMeetingProgram.Text = ReplaceNull(program ?? string.Empty);
        txtMeetingScribeEnabled.Text = ReplaceNull(scribeEnabled ?? string.Empty);
        txtMeetingCheckedInByFrontDesk.Text = ReplaceNull(checkedInByFrontDesk ?? string.Empty);

        // Get and display meeting error if it exists
        var meetingError = TransMorgDb.GetMeetingError(selectedMeeting.MeetingId);
        if (meetingError != null)
        {
            var kind = meetingError.Value.TryGetProperty("Kind", out var kindElem)
                ? (kindElem.GetString() ?? string.Empty) : string.Empty;
            var reason = meetingError.Value.TryGetProperty("Reason", out var reasonElem)
                ? (reasonElem.GetString() ?? string.Empty) : string.Empty;

            if (!string.IsNullOrWhiteSpace(kind) || !string.IsNullOrWhiteSpace(reason))
            {
                txtMeetingError.Text = $"{kind}\n{reason}";
            }
            else
            {
                txtMeetingError.Text = "---";
            }
        }
        else
        {
            txtMeetingError.Text = "---";
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
        var patientDetailsForQuality = TransMorgDb.GetPatientDetails(_currentPatientName, _currentPatientId);

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
        txtPatientArrived.Text = ReplaceNull(arrived);
        txtPatientDropped.Text = ReplaceNull(dropped);
        txtPatientDuration.Text = ReplaceNull(patientDuration);
        txtPatientRating.Text = ReplaceNull(rating);
        txtCheckInViaChat.Text = ReplaceNull(checkInViaChat);
        txtCheckInWait.Text = ReplaceNull(checkInWait);
        txtWaitForCareTeam.Text = ReplaceNull(waitForCareTeam);
        txtWaitForProvider.Text = ReplaceNull(waitForProvider);
        txtCheckOutWait.Text = ReplaceNull(checkOutWait);
        txtPatientDevice.Text = ReplaceNull(device);
        txtPatientOs.Text = ReplaceNull(os);
        txtPatientBrowser.Text = ReplaceNull(browser);
        txtMeetingQualityData.Text = ReplaceNull(qualityData);

        // Show the meeting details section
        spnlMeetingDetails.Visibility = Visibility.Visible;
    }

    /*
     * EVENT HANDLERS
     */
    private void btnSearchToggle_Click(object? sender, RoutedEventArgs e) => SearchToggleClicked();

    private void txbxSearch_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e) => SearchTextChanged();

    private void rbtnSearch_Checked(object sender, RoutedEventArgs e) => SearchTextChanged();

    private void lstbxSearchResults_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e) => SearchResultSelected();

    private void dgPatientMeetings_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e) => MeetingSelected();
}