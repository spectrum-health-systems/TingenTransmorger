// 260218_code
// 260218_documentation

using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using TingenTransmorger.Models;

namespace TingenTransmorger;

/* The MainWindow.DetailDisplay partial class contains logic related to displaying patient/provider details in the UI.
 */
public partial class MainWindow : Window
{
    /// <summary>SMS failure records for the current patient's phone numbers.</summary>
    private List<(string PhoneNumber, string ErrorMessage, string ScheduledStartTime)> _smsFailures = new();

    /// <summary>Message delivery records for the current patient's phone numbers.</summary>
    private List<(string PhoneNumber, string DeliveryStatus, string MessageType, string ErrorMessage, string DateSent, string TimeSent)> _smsDeliveries = new();

    /// <summary>Email failure records for the current patient's email addresses.</summary>
    private List<(string EmailAddress, string ErrorMessage, string ScheduledStartTime)> _emailFailures = new();

    /// <summary>Email delivery records for the current patient's email addresses.</summary>
    private List<(string EmailAddress, string DeliveryStatus, string MessageType, string ErrorMessage, string DateSent, string TimeSent)> _emailDeliveries = new();

    /// <summary> Currently selected patient name.</summary>
    private string _currentPatientName = string.Empty;

    /// <summary>Currently selected patient ID.</summary>
    private string _currentPatientId = string.Empty;

    /* SEARCH RESULTS */

    /// <summary>Modifies the search results based on the current search type and search text.</summary>
    private void ModifySearchResults()
    {
        var searchResults = GetSearchResults(btnSearchToggle.Content.ToString(), txbxSearchBox.Text?.Trim());
        DisplaySearchResults(searchResults);
    }

    /// <summary>Get a list of patient/provider search results.</summary>
    /// <param name="searchType">The type of search.</param>
    /// <param name="searchText">Contents of the search box.</param>
    /// <returns>The search results.</returns>
    private List<string> GetSearchResults(string searchType, string searchText)
    {
        if (string.IsNullOrWhiteSpace(txbxSearchBox.Text))
        {
            return [];
        }

        /* If the search box contains only an asterisk, treat it as a wildcard to return all results
         */
        if (txbxSearchBox.Text == "*")
        {
            searchText = string.Empty;
        }

        return searchType.Contains("patient", StringComparison.OrdinalIgnoreCase)
            ? rbtnSearchByName.IsChecked == true
                ? Database.SearchFor.PatientByName(searchText, TmDb)
                : Database.SearchFor.PatientById(searchText, TmDb)
            : searchType.Contains("provider", StringComparison.OrdinalIgnoreCase)
                ? rbtnSearchByName.IsChecked == true
                            ? Database.SearchFor.ProviderByName(searchText, TmDb)
                            : Database.SearchFor.ProviderById(searchText, TmDb)
                : [];
    }

    /// <summary>Display search results.</summary>
    /// <param name="searchResults">The list of search results to display.</param>
    private void DisplaySearchResults(List<string> searchResults)
    {
        lstbxSearchResults.Items.Clear();

        if (searchResults.Count != 0)
        {
            foreach (string result in searchResults)
            {
                lstbxSearchResults.Items.Add(result);
            }
        }
    }

    /* DETAILS */

    private void ModifyDetails()
    {
        var selectedItem = lstbxSearchResults.SelectedItem as string;

        /* This is here so we don't try and get details when there are not search results.
        */
        if (lstbxSearchResults.Items.Count == 0)
        {
            return;
        }

        var lastParenIndex = selectedItem.LastIndexOf('(');
        var name           = selectedItem.Substring(0, lastParenIndex).Trim();
        var id             = selectedItem.Substring(lastParenIndex + 1).TrimEnd(')').Trim();

        if (btnSearchToggle.Content.ToString().Contains("patient", StringComparison.OrdinalIgnoreCase))
        {
            DisplayPatientDetails(name, id);
        }
        else if (btnSearchToggle.Content.ToString().Contains("provider", StringComparison.OrdinalIgnoreCase))
        {
            DisplayProviderDetails(name, id);
        }
    }

    /* PATIENT DETAILS */

    /// <summary>Displays patient details in the UI.</summary>
    private void DisplayPatientDetails(string patientName, string patientId)
    {
        _currentPatientName = patientName;
        _currentPatientId   = patientId;

        JsonElement? patientDetails = TmDb.GetPatientDetails(patientName, patientId);

        if (patientDetails == null)
        {
            StopApp($"Critical error! [ERR-8151]");
        }

        SetupPatientDetailUi(patientName, patientId);
        ModifyPatientPhoneNumber(patientDetails);
        DisplayPatientEmailAddress(patientDetails);
        DisplayPatientMeetings(patientDetails);
    }

    private void ModifyPatientPhoneNumber(JsonElement? patientDetails)
    {
        var phoneNumbers = GetPatientPhoneNumbers(patientDetails);
        ShowPatientPhoneNumber(phoneNumbers);
        GetSmsStats(phoneNumbers);
        UpdateDetailsButtonColor(_smsFailures.Count > 0, _smsDeliveries.Count > 0, btnPhoneDetails);
    }

    private static List<string> GetPatientPhoneNumbers(JsonElement? patientDetails)
    {
        var phoneNumbers = new List<string>();

        if (patientDetails?.TryGetProperty("PhoneNumbers", out var phoneNumbersArray) == true && phoneNumbersArray.ValueKind == JsonValueKind.Array)
        {
            foreach (var phoneNumberEntry in phoneNumbersArray.EnumerateArray())
            {
                if (phoneNumberEntry.TryGetProperty("Number", out var number))
                {
                    var phoneNumber = number.GetString();

                    if (!string.IsNullOrWhiteSpace(phoneNumber))
                    {
                        var phoneNumberDigits = new string(phoneNumber.Where(char.IsDigit).ToArray());

                        if (phoneNumberDigits.Length == 10)
                        {
                            phoneNumber = $"{phoneNumberDigits.Substring(0, 3)}-{phoneNumberDigits.Substring(3, 3)}-{phoneNumberDigits.Substring(6, 4)}"; // Format as ###-###-#### if 10 digits
                        }

                        phoneNumbers.Add(phoneNumber);
                    }
                }
            }
        }

        return phoneNumbers;
    }

    private void ShowPatientPhoneNumber(List<string> phoneNumbers)
    {
        lblPatientPhoneValue.Content = phoneNumbers.Count > 0
            ? string.Join(", ", phoneNumbers)
            : "No phone numbers on file";
    }

    private void GetSmsStats(List<string> phoneNumbers)
    {
        // Query SMS failure and delivery stats for all patient phone numbers
        _smsFailures.Clear();
        _smsDeliveries.Clear();

        for (int i = 0; i < phoneNumbers.Count; i++)
        {
            var normalizedPhoneNumber = new string(phoneNumbers[i].Where(char.IsDigit).ToArray()).Trim();

            if (normalizedPhoneNumber.Length == 10)
            {
                // Query SMS failures
                var failures = TmDb.GetSmsFailureStats(normalizedPhoneNumber);
                _smsFailures.AddRange(failures);

                // Query message deliveries
                var deliveries = TmDb.GetMessageDeliveryStats(normalizedPhoneNumber);
                _smsDeliveries.AddRange(deliveries);
            }
        }
    }

    private void DisplayPatientEmailAddress(JsonElement? patientDetails)
    {
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
                var failures = TmDb.GetEmailFailureStats(emailAddress);
                System.Diagnostics.Debug.WriteLine($"Found {failures.Count} email failures");
                _emailFailures.AddRange(failures);

                // Query email deliveries
                var deliveries = TmDb.GetEmailDeliveryStats(emailAddress);
                System.Diagnostics.Debug.WriteLine($"Found {deliveries.Count} email deliveries");
                _emailDeliveries.AddRange(deliveries);
            }
        }


        // Update btnEmailDetails button based on email records
        UpdateDetailsButtonColor(_emailFailures.Count > 0, _emailDeliveries.Count > 0, btnEmailDetails);
    }




    private void DisplayPatientMeetings(JsonElement? patientDetails)
    {
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
                    var meetingDetail = TmDb.GetMeetingDetail(meetingId);
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
                    var hasError = TmDb.HasMeetingError(meetingId);

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
        txbkTotalMeetingsValue.Text = $"{totalCount} MEETINGS";
        txbkCompletedMeetingsValue.Text = $"{completedCount} Completed";
        txbkMeetingsInProgressValue.Text = $"{inProgressCount} In-Progress";
        txbkMeetingsExpiredValue.Text = $"{expiredCount} Expired";
        txbkMeetingsCancelledValue.Text = $"{cancelledCount} Cancelled";
        txbkMeetingsScheduledValue.Text = $"{scheduledCount} Scheduled";

        // Bind to DataGrid
        dgrdMeetingResults.ItemsSource = meetingRows;

        // Show meetings section if there are meetings
        if (meetingRows.Count > 0)
        {
            spnlMeetingComponents.Visibility = Visibility.Visible;
        }
        else
        {
            spnlMeetingComponents.Visibility = Visibility.Collapsed;
        }

        // Hide meeting details until a meeting is selected
        spnlMeetingDetailsComponents.Visibility = Visibility.Collapsed;
    }



    private void ShowPhoneDetails()
    {
        var messageHistoryWindow = new Database.MessageHistoryWindow(_smsFailures, _smsDeliveries)
        {
            Owner = this
        };

        messageHistoryWindow.ShowDialog();
    }




    /// <summary>Updates the btnPhoneDetails button appearance based on SMS failure and delivery records.</summary>
    private void UpdateDetailsButtonColor(bool hasFailures, bool hasDeliveries, Button theButton)
    {
        theButton.IsEnabled = true;

        if (hasFailures && hasDeliveries)
        {
            theButton.Background = System.Windows.Media.Brushes.Yellow;
        }
        else if (hasDeliveries)
        {
            theButton.Background = System.Windows.Media.Brushes.Green;
        }
        else if (hasFailures)
        {
            theButton.Background = System.Windows.Media.Brushes.Red;
        }
        else
        {
            // No records: gray background, disabled
            theButton.Background = System.Windows.Media.Brushes.Gray;
            theButton.IsEnabled = false;
        }
    }


    /// <summary>Handles the email details button click event.</summary>
    private void ShowEmailDetails()
    {
        var emailHistoryWindow = new Database.MessageHistoryWindow(_emailFailures, _emailDeliveries)
        {
            Owner = this
        };

        emailHistoryWindow.ShowDialog();
    }





















    /// <summary>Displays provider details in the UI.</summary>
    private void DisplayProviderDetails(string providerName, string providerId)
    {
        // Show provider details section
        spnlPatientProviderDetailsComponents.Visibility = Visibility.Visible;

        // Set header to PROVIDER
        lblPatientProviderKey.Content = "PROVIDER";

        /* TODO: These are related to the potentially unused fields at the top of this class.
         */
        //////// Store current provider info
        //////_currentProviderName = providerName;
        //////_currentProviderId = providerId;

        // Get provider details from database
        var providerDetails = TmDb.GetProviderDetails(providerName);
        if (providerDetails == null)
        {
            return;
        }

        // Display provider name and ID
        lblPatientProviderNameValue.Content = providerName;
        lblPatientProviderIdValue.Content = providerId;

        // Hide phone and email sections for providers
        spnlPatientPhoneComponents.Visibility = Visibility.Collapsed;
        spnlPatientEmailComponents.Visibility = Visibility.Collapsed;

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
                var failures = TmDb.GetEmailFailureStats(emailAddress);
                System.Diagnostics.Debug.WriteLine($"Found {failures.Count} email failures");
                _emailFailures.AddRange(failures);

                // Query email deliveries
                var deliveries = TmDb.GetEmailDeliveryStats(emailAddress);
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
                    var meetingDetail = TmDb.GetMeetingDetail(meetingId);
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
                    var hasError = TmDb.HasMeetingError(meetingId);

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
        txbkTotalMeetingsValue.Text = $"{totalCount} MEETINGS";
        txbkCompletedMeetingsValue.Text = $"{completedCount} Completed";
        txbkMeetingsInProgressValue.Text = $"{inProgressCount} In-Progress";
        txbkMeetingsExpiredValue.Text = $"{expiredCount} Expired";
        txbkMeetingsCancelledValue.Text = $"{cancelledCount} Cancelled";
        txbkMeetingsScheduledValue.Text = $"{scheduledCount} Scheduled";

        // Bind to DataGrid
        dgrdMeetingResults.ItemsSource = meetingRows;

        // Show meetings section if there are meetings
        if (meetingRows.Count > 0)
        {
            spnlMeetingComponents.Visibility = Visibility.Visible;
        }
        else
        {
            spnlMeetingComponents.Visibility = Visibility.Collapsed;
        }

        // Hide meeting details until a meeting is selected
        spnlMeetingDetailsComponents.Visibility = Visibility.Collapsed;
    }
}