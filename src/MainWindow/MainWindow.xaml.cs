// 260212_code
// 260212_documentation

/* The MainWindow class contains the following partial classes:
 *   - MainWindow.asmx         - MainWindow XAML markup
 *   - MainWindow.asmx.cs      - MainWindow general logic
 *   - MainWindow.AdminMode.cs - Logic related to admin mode functionality
 */

using System.IO;
using System.Text.Json;
using System.Windows;
using TingenTransmorger.Core;
using TingenTransmorger.Database;
using TingenTransmorger.Models;

/* I've moved the MainWindow partial classes to MainWindow/ to keep the code organized, but I'm leaving the namespace as
 * TingenTransmorger instead of TingenTransmorger.MainWindow to avoid confusion with the MainWindow class.
 */
namespace TingenTransmorger;

/// <summary>Entry class for Tingen Transmorger.</summary>
public partial class MainWindow : Window
{
    /// <summary>The Transmorger database.</summary>
    /// <remarks>Defined here so it can be used throughout the application.</remarks>
    public TransmorgerDatabase tmDb { get; set; }

    /// <summary> Currently selected patient name.</summary>
    private string _currentPatientName = string.Empty;

    /// <summary>Currently selected patient ID.</summary>
    private string _currentPatientId = string.Empty;

    /* TODO: The provider details section is currently not implemented in the UI, so we may not either of these.
     */
    /////// <summary>Currently selected provider name.</summary>
    ////private string _currentProviderName = string.Empty;
    /////// <summary>Currently selected provider ID.</summary>
    ////private string _currentProviderId = string.Empty;

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
        /* TODO: Make sure this is verified properly.
         */
        var config = Configuration.Load();

        /* TODO: Verify this is working. If the config file doesn't have an Import path, the app crashes.
         */
        Framework.Verify(config);

        /* If the mode is set to Admin, let's do some admin stuff before we load the database.
         */
        if (string.Equals(config.Mode.Trim(), "admin", StringComparison.OrdinalIgnoreCase))
        {
            var flowControl = await EnterAdminMode(config.AdminDirectories["Import"],
                                                   config.AdminDirectories["Tmp"],
                                                   config.StandardDirectories["MasterDb"]);

            if (!flowControl)
            {
                return;
            }
        }

        string localDbPath  = Path.Combine(config.StandardDirectories["LocalDb"], "transmorger.db");
        string masterDbPath = Path.Combine(config.StandardDirectories["MasterDb"], "transmorger.db");

        TransmorgerDatabase.Update(localDbPath, masterDbPath);

        tmDb = TransmorgerDatabase.Load(localDbPath);

        SetupInitialUI();
    }

    /// <summary> Stops the application. </summary>
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







    /// <summary>Searches for patients based on the search text.</summary>
    private void SearchPatients(string searchText)
    {
        // Get all patients from the database
        var allPatients = tmDb.GetPatients();

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

    /// <summary>Searches for providers based on the search text.</summary>
    private void SearchProviders(string searchText)
    {
        // Get all providers from the database
        var allProviders = tmDb.GetProviders();

        // Filter providers based on search type
        var filteredProviders = new List<(string ProviderName, string ProviderId)>();

        if (rbtnByName.IsChecked == true)
        {
            // Search by name - case insensitive
            filteredProviders = allProviders
                .Where(p => p.ProviderName.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                .OrderBy(p => p.ProviderName)
                .ToList();
        }
        else if (rbtnById.IsChecked == true)
        {
            // Search by ID
            filteredProviders = allProviders
                .Where(p => p.ProviderId.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                .OrderBy(p => p.ProviderName)
                .ToList();
        }

        // Display results in the format "ProviderName (ProviderId)"
        foreach (var provider in filteredProviders)
        {
            var displayId = provider.ProviderId == "N/A" ? "No ID" : provider.ProviderId;
            lstbxSearchResults.Items.Add($"{provider.ProviderName} ({displayId})");
        }
    }

    /// <summary>Handles the selection changed event for the search results list.</summary>
    /// <remarks>
    /// <para>
    /// This method is called when the user selects a patient or provider from the search results.
    /// It retrieves the full details and displays them in the details panel.
    /// </para>
    /// </remarks>
    private void SearchResultSelected()
    {
        // Don't process selection if database is not yet initialized
        if (tmDb == null)
        {
            return;
        }

        var searchMode = btnSearchToggle.Content.ToString();

        // Get the selected item
        var selectedItem = lstbxSearchResults.SelectedItem as string;
        if (string.IsNullOrWhiteSpace(selectedItem))
        {
            return;
        }

        // Parse the selected item to extract Name and ID
        // Format is "Name (ID)"
        var lastParenIndex = selectedItem.LastIndexOf('(');
        if (lastParenIndex == -1)
        {
            return;
        }

        var name = selectedItem.Substring(0, lastParenIndex).Trim();
        var id = selectedItem.Substring(lastParenIndex + 1).TrimEnd(')').Trim();

        if (searchMode == "Patient Search")
        {
            DisplayPatientDetails(name, id);
        }
        else if (searchMode == "Provider Search")
        {
            DisplayProviderDetails(name, id);
        }
    }

    /// <summary>Displays patient details in the UI.</summary>
    private void DisplayPatientDetails(string patientName, string patientId)
    {
        // Show patient details section
        spnlPatientDetails.Visibility = Visibility.Visible;

        // Set header to PATIENT
        lblPatientHeader.Content = "PATIENT";

        // Store current patient info
        _currentPatientName = patientName;
        _currentPatientId = patientId;

        // Get patient details from database
        var patientDetails = tmDb.GetPatientDetails(patientName, patientId);
        if (patientDetails == null)
        {
            return;
        }

        // Display patient name and ID
        lblPatientNameValue.Content = patientName;
        lblPatientIdValue.Content = patientId;

        // Show phone and email sections
        spnlPatientPhone.Visibility = Visibility.Visible;
        spnlPatientEmail.Visibility = Visibility.Visible;

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
                            // Remove non-digits
                            var digits = new string(number.Where(char.IsDigit).ToArray());
                            // Format as ###-###-#### if 10 digits
                            if (digits.Length == 10)
                            {
                                number = $"{digits.Substring(0, 3)}-{digits.Substring(3, 3)}-{digits.Substring(6, 4)}";
                            }
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

        // Query SMS failure and delivery stats for all patient phone numbers
        _smsFailures.Clear();
        _smsDeliveries.Clear();

        foreach (var phoneNumber in phoneNumbers)
        {
            if (phoneNumber != "No phone numbers on file")
            {
                // Get normalized phone number (digits only)
                var normalizedPhone = new string(phoneNumber.Where(char.IsDigit).ToArray());

                if (normalizedPhone.Length == 10)
                {
                    // DEBUG: Show what we're searching for
                    System.Diagnostics.Debug.WriteLine($"Searching for phone: {normalizedPhone}");

                    // Query SMS failures
                    var failures = tmDb.GetSmsFailureStats(normalizedPhone);
                    System.Diagnostics.Debug.WriteLine($"Found {failures.Count} SMS failures");
                    _smsFailures.AddRange(failures);

                    // Query message deliveries
                    var deliveries = tmDb.GetMessageDeliveryStats(normalizedPhone);
                    System.Diagnostics.Debug.WriteLine($"Found {deliveries.Count} message deliveries");
                    _smsDeliveries.AddRange(deliveries);
                }
            }
        }

        System.Diagnostics.Debug.WriteLine($"Total failures: {_smsFailures.Count}, Total deliveries: {_smsDeliveries.Count}");

        // Update btnPhoneDetails button based on SMS records
        UpdatePhoneDetailsButton();

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

        // Query email failure and delivery stats for all patient email addresses
        _emailFailures.Clear();
        _emailDeliveries.Clear();

        foreach (var emailAddress in emailAddresses)
        {
            if (emailAddress != "No email addresses on file")
            {
                // DEBUG: Show what we're searching for
                System.Diagnostics.Debug.WriteLine($"Searching for email: {emailAddress}");

                // Query email failures
                var failures = tmDb.GetEmailFailureStats(emailAddress);
                System.Diagnostics.Debug.WriteLine($"Found {failures.Count} email failures");
                _emailFailures.AddRange(failures);

                // Query email deliveries
                var deliveries = tmDb.GetEmailDeliveryStats(emailAddress);
                System.Diagnostics.Debug.WriteLine($"Found {deliveries.Count} email deliveries");
                _emailDeliveries.AddRange(deliveries);
            }
        }

        System.Diagnostics.Debug.WriteLine($"Total email failures: {_emailFailures.Count}, Total email deliveries: {_emailDeliveries.Count}");

        // Update btnEmailDetails button based on email records
        UpdateEmailDetailsButton();

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
                    var meetingDetail = tmDb.GetMeetingDetail(meetingId);
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
                    var hasError = tmDb.HasMeetingError(meetingId);

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

    /// <summary>Displays provider details in the UI.</summary>
    private void DisplayProviderDetails(string providerName, string providerId)
    {
        // Show provider details section
        spnlPatientDetails.Visibility = Visibility.Visible;

        // Set header to PROVIDER
        lblPatientHeader.Content = "PROVIDER";

        /* TODO: These are related to the potentially unused fields at the top of this class.
         */
        //////// Store current provider info
        //////_currentProviderName = providerName;
        //////_currentProviderId = providerId;

        // Get provider details from database
        var providerDetails = tmDb.GetProviderDetails(providerName);
        if (providerDetails == null)
        {
            return;
        }

        // Display provider name and ID
        lblPatientNameValue.Content = providerName;
        lblPatientIdValue.Content = providerId;

        // Hide phone and email sections for providers
        spnlPatientPhone.Visibility = Visibility.Collapsed;
        spnlPatientEmail.Visibility = Visibility.Collapsed;

        // Still collect email data in the background (hidden from UI)
        // Display email addresses (hidden from user but still processed)
        var emailAddresses = new List<string>();
        if (providerDetails.Value.TryGetProperty("EmailAddresses", out var emailAddressesArray))
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

        // Query email failure and delivery stats for all provider email addresses (background processing)
        _emailFailures.Clear();
        _emailDeliveries.Clear();

        foreach (var emailAddress in emailAddresses)
        {
            if (emailAddress != "No email addresses on file")
            {
                // DEBUG: Show what we're searching for
                System.Diagnostics.Debug.WriteLine($"Searching for provider email: {emailAddress}");

                // Query email failures
                var failures = tmDb.GetEmailFailureStats(emailAddress);
                System.Diagnostics.Debug.WriteLine($"Found {failures.Count} email failures");
                _emailFailures.AddRange(failures);

                // Query email deliveries
                var deliveries = tmDb.GetEmailDeliveryStats(emailAddress);
                System.Diagnostics.Debug.WriteLine($"Found {deliveries.Count} email deliveries");
                _emailDeliveries.AddRange(deliveries);
            }
        }

        System.Diagnostics.Debug.WriteLine($"Total provider email failures: {_emailFailures.Count}, Total provider email deliveries: {_emailDeliveries.Count}");

        // Display meetings for this provider
        var meetingRows = new List<PatientMeetingRow>();
        if (providerDetails.Value.TryGetProperty("Meetings", out var meetingsElement))
        {
            if (meetingsElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var meetingIdElement in meetingsElement.EnumerateArray())
                {
                    var meetingId = meetingIdElement.GetString();
                    if (string.IsNullOrWhiteSpace(meetingId))
                        continue;

                    // Get meeting details from MeetingDetail
                    var meetingDetail = tmDb.GetMeetingDetail(meetingId);
                    if (meetingDetail == null)
                        continue;

                    // Get meeting information
                    var scheduledStart = meetingDetail.Value.TryGetProperty("ScheduledStart", out var startElem)
                        ? startElem.GetString() : string.Empty;
                    var status = meetingDetail.Value.TryGetProperty("Status", out var statusElem)
                        ? statusElem.GetString() : string.Empty;
                    var duration = meetingDetail.Value.TryGetProperty("Duration", out var durationElem)
                        ? durationElem.GetString() : string.Empty;

                    // For providers, we don't have patient-specific arrival/drop times
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

                    // Check if meeting has an error
                    var hasError = tmDb.HasMeetingError(meetingId);

                    // Check status flags
                    var statusLower = status?.ToLower() ?? string.Empty;
                    var isCancelled = statusLower.Contains("cancel");
                    var isCompleted = statusLower.Contains("complete");

                    meetingRows.Add(new PatientMeetingRow
                    {
                        MeetingId = meetingId,
                        Start = ReplaceNull(scheduledStart ?? string.Empty),
                        Arrived = "N/A",  // Not applicable for provider view
                        Dropped = "N/A",  // Not applicable for provider view
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

        var inProgressCount = 0;
        var expiredCount = 0;
        var scheduledCount = 0;

        foreach (var meeting in meetingRows)
        {
            var statusLower = meeting.Status?.ToLower() ?? string.Empty;

            if (meeting.IsCompleted || meeting.IsCancelled)
                continue;

            if (statusLower.Contains("in progress") || statusLower.Contains("in-progress"))
                inProgressCount++;
            else if (statusLower.Contains("expired"))
                expiredCount++;
            else if (statusLower.Contains("scheduled"))
                scheduledCount++;
        }

        // Update the header with the detailed count
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
        // Don't process selection if database is not yet initialized
        if (tmDb == null)
        {
            spnlMeetingDetails.Visibility = Visibility.Collapsed;
            return;
        }

        // Get the selected meeting
        var selectedMeeting = dgPatientMeetings.SelectedItem as PatientMeetingRow;
        if (selectedMeeting == null || string.IsNullOrWhiteSpace(selectedMeeting.MeetingId))
        {
            spnlMeetingDetails.Visibility = Visibility.Collapsed;
            return;
        }

        // Get meeting details from database
        var meetingDetail = tmDb.GetMeetingDetail(selectedMeeting.MeetingId);
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


        // Extract workflow, program, and front desk check-in
        var workflow = meetingDetail.Value.TryGetProperty("Workflow", out var workflowElem)
            ? (workflowElem.GetString() ?? string.Empty) : string.Empty;
        var program = meetingDetail.Value.TryGetProperty("Program", out var programElem)
            ? (programElem.GetString() ?? string.Empty) : string.Empty;
        var checkedInByFrontDesk = meetingDetail.Value.TryGetProperty("CheckedInByFrontDesk", out var checkedInElem)
            ? (checkedInElem.GetString() ?? string.Empty) : string.Empty;

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
        txtMeetingCheckedInByFrontDesk.Text = ReplaceNull(checkedInByFrontDesk ?? string.Empty);

        // Get and display meeting error if it exists
        var meetingError = tmDb.GetMeetingError(selectedMeeting.MeetingId);
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
        var patientDetailsForQuality = tmDb.GetPatientDetails(_currentPatientName, _currentPatientId);

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

        // Show/hide patient-specific and provider-specific meeting details based on current view mode
        // If we're viewing a provider, hide the patient-specific section and show provider section
        if (lblPatientHeader.Content?.ToString() == "PROVIDER")
        {
            brdrMeetingDetailsPatient.Visibility = Visibility.Collapsed;
            brdrMeetingDetailsProvider.Visibility = Visibility.Visible;

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
            brdrMeetingDetailsPatient.Visibility = Visibility.Visible;
            brdrMeetingDetailsProvider.Visibility = Visibility.Collapsed;
        }

        // Show the meeting details section
        spnlMeetingDetails.Visibility = Visibility.Visible;
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
            sb.AppendLine("Meeting ID:       " + lblMeetingIdValue.Text);
            sb.AppendLine("Title:            " + lblMeetingTitleValue.Text);
            sb.AppendLine("Status:           " + lblMeetingStatusValue.Text);
            sb.AppendLine("Joins:            " + lblMeetingJoins.Text);
            sb.AppendLine("Duration:         " + lblMeetingDuration.Text);
            sb.AppendLine("Service code:     " + lblMeetingServiceCode.Text);
            sb.AppendLine();

            // Center column
            sb.AppendLine("Started by:       " + lblMeetingInitiatedBy.Text);
            sb.AppendLine("Scheduled start:  " + lblMeetingScheduledStart.Text);
            sb.AppendLine("Actual start:     " + lblMeetingActualStart.Text);
            sb.AppendLine("Ended by:         " + lblMeetingEndedBy.Text);
            sb.AppendLine("Scheduled end:    " + lblMeetingScheduledEnd.Text);
            sb.AppendLine("Actual end:       " + lblMeetingActualEnd.Text);
            sb.AppendLine();

            // Right column
            sb.AppendLine("Workflow:         " + txtMeetingWorkflow.Text);
            sb.AppendLine("Program:          " + txtMeetingProgram.Text);
            sb.AppendLine("Front Desk Check-In: " + txtMeetingCheckedInByFrontDesk.Text);
            sb.AppendLine();
            sb.AppendLine("Meeting error:");
            sb.AppendLine(txtMeetingError.Text);

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
            sb.AppendLine("Patient arrived:  " + txtPatientArrived.Text);
            sb.AppendLine("Patient dropped:  " + txtPatientDropped.Text);
            sb.AppendLine("Duration:         " + txtPatientDuration.Text);
            sb.AppendLine("Rating:           " + txtPatientRating.Text);
            sb.AppendLine();

            // Center column
            sb.AppendLine("Checked-In via chat: " + txtCheckInViaChat.Text);
            sb.AppendLine("Check-In wait:    " + txtCheckInWait.Text);
            sb.AppendLine("Wait for Care Team: " + txtWaitForCareTeam.Text);
            sb.AppendLine("Wait for provider: " + txtWaitForProvider.Text);
            sb.AppendLine("Check-out wait:   " + txtCheckOutWait.Text);
            sb.AppendLine();

            // Right column
            sb.AppendLine("Device:           " + txtPatientDevice.Text);
            sb.AppendLine("OS:               " + txtPatientOs.Text);
            sb.AppendLine("Browser:          " + txtPatientBrowser.Text);
            sb.AppendLine();

            // Quality data (spanning section)
            sb.AppendLine("Participant Meeting Quality Data:");
            sb.AppendLine(txtMeetingQualityData.Text);

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