// 260218_code
// 260218_documentation

using System.Text.Json;
using System.Windows;
using TingenTransmorger.Models;

namespace TingenTransmorger;

/* Partial class MainWindow.DetailDisplay.cs.
 */
public partial class MainWindow : Window
{
    /// <summary> Currently selected patient name.</summary>
    private string _currentPatientName = string.Empty;

    /// <summary>Currently selected patient ID.</summary>
    private string _currentPatientId = string.Empty;

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

        DisplayPhoneNumber(patientDetails);


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

    /// <summary>
    ///
    /// </summary>
    /// <param name="patientDetails"></param>
    private void DisplayPhoneNumber(JsonElement? patientDetails)
    {
        var phoneNumbers     = new List<string>();
        var normalizedPhones = new List<string>();

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
                            var digits = new string(number.Where(char.IsDigit).ToArray()); // Remove non-digits

                            if (digits.Length == 10)
                            {
                                number = $"{digits.Substring(0, 3)}-{digits.Substring(3, 3)}-{digits.Substring(6, 4)}"; // Format as ###-###-#### if 10 digits
                                normalizedPhones.Add(digits);
                            }
                            else
                            {
                                normalizedPhones.Add(digits);
                            }

                            phoneNumbers.Add(number);
                        }
                    }
                }
            }
        }

        lblPatientPhoneValue.Content = phoneNumbers.Count > 0
            ? string.Join(", ", phoneNumbers)
            : "No phone numbers on file";

        // Query SMS failure and delivery stats for all patient phone numbers
        _smsFailures.Clear();
        _smsDeliveries.Clear();

        for (int i = 0; i < normalizedPhones.Count; i++)
        {
            if (normalizedPhones[i].Length == 10)
            {
                // Query SMS failures
                var failures = TmDb.GetSmsFailureStats(normalizedPhones[i]);
                _smsFailures.AddRange(failures);

                // Query message deliveries
                var deliveries = TmDb.GetMessageDeliveryStats(normalizedPhones[i]);
                _smsDeliveries.AddRange(deliveries);
            }
        }

        System.Diagnostics.Debug.WriteLine($"Total failures: {_smsFailures.Count}, Total deliveries: {_smsDeliveries.Count}");

        UpdatePhoneDetailsButton();
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