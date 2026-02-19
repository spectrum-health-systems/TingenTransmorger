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
    /// <summary> Currently selected patient name.</summary>
    private string _currentPatientName = string.Empty;

    /// <summary>Currently selected patient ID.</summary>
    private string _currentPatientId = string.Empty;

    /// <summary>Message delivery records for the current patient's phone numbers.</summary>
    private List<(string PhoneNumber, string DeliveryStatus, string MessageType, string ErrorMessage, string DateSent, string TimeSent)> _smsDeliveries = [];

    /// <summary>Email delivery records for the current patient's email addresses.</summary>
    private List<(string EmailAddress, string DeliveryStatus, string MessageType, string ErrorMessage, string DateSent, string TimeSent)> _emailDeliveries = [];

    /// <summary>SMS failure records for the current patient's phone numbers.</summary>
    private List<(string PhoneNumber, string ErrorMessage, string ScheduledStartTime)> _smsFailures =[];

    /// <summary>Email failure records for the current patient's email addresses.</summary>
    private List<(string EmailAddress, string ErrorMessage, string ScheduledStartTime)> _emailFailures = [];

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
        DisplayPatientPhoneNumber(patientDetails);
        DisplayPatientEmailAddress(patientDetails);
        DisplayMeetingResults(patientDetails);
    }

    /// <summary>Displays the patient's phone numbers in the UI.</summary>
    /// <param name="patientDetails">The JSON element containing the patient's details.</param>
    private void DisplayPatientPhoneNumber(JsonElement? patientDetails)
    {
        var phoneNumbers = GetPatientPhoneNumbers(patientDetails);

        ShowPatientPhoneNumber(phoneNumbers);
        GetSmsStats(phoneNumbers);
        UpdateDetailsButtonColor(_smsFailures.Count > 0, _smsDeliveries.Count > 0, btnPhoneDetails);
    }

    /// <summary>Get a list of formatted phone numbers for a patient.</summary>
    /// <param name="patientDetails">The JSON representation of the patient's details.</param>
    /// <returns>A list of strings representing the formatted phone numbers of the patient.</returns>
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

    /// <summary>Show the patient phone number.</summary>
    /// <param name="phoneNumbers">A list of formatted phone numbers for the patient.</param>
    private void ShowPatientPhoneNumber(List<string> phoneNumbers)
    {
        lblPatientPhoneValue.Content = phoneNumbers.Count > 0
            ? string.Join(", ", phoneNumbers)
            : "No phone numbers on file";
    }

    /// <summary>Gets the SMS statistics for the provided phone numbers.</summary>
    /// <param name="phoneNumbers">A list of formatted phone numbers for the patient.</param>
    private void GetSmsStats(List<string> phoneNumbers)
    {
        _smsFailures.Clear();
        _smsDeliveries.Clear();

        for (int i = 0; i < phoneNumbers.Count; i++)
        {
            var normalizedPhoneNumber = new string(phoneNumbers[i].Where(char.IsDigit).ToArray()).Trim();

            if (normalizedPhoneNumber.Length == 10)
            {
                var failures = TmDb.GetSmsFailureStats(normalizedPhoneNumber);
                _smsFailures.AddRange(failures);

                var deliveries = TmDb.GetMessageDeliveryStats(normalizedPhoneNumber);
                _smsDeliveries.AddRange(deliveries);
            }
        }
    }
    /// <summary>Gets the patient's email addresses from the patient details and displays them in the UI.</summary>
    /// <param name="patientDetails">The JSON element containing the patient's details.</param>
    private void DisplayPatientEmailAddress(JsonElement? patientDetails)
    {
        var emailAddresses = GetPatientEmailAddresses(patientDetails);

        ShowPatientEmailAddress(emailAddresses);
        GetEmailStats(emailAddresses);
        UpdateDetailsButtonColor(_emailFailures.Count > 0, _emailDeliveries.Count > 0, btnEmailDetails);
    }

    /// <summary>
    /// Retrieves a list of email addresses associated with the specified patient details.
    /// </summary>
    /// <remarks>This method extracts email addresses from the provided JSON object. It only includes
    /// non-empty addresses in the returned list.</remarks>
    /// <param name="patientDetails">A JSON element representing the patient's details. This element is expected to contain an 'EmailAddresses'
    /// property with an array of email address objects. This parameter cannot be null.</param>
    /// <returns>A list of strings containing the email addresses of the patient. The list will be empty if no email addresses
    /// are found.</returns>
    private static List<string> GetPatientEmailAddresses(JsonElement? patientDetails)
    {
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

        return emailAddresses;
    }

    /// <summary>Gets the email statistics for the provided email addresses.</summary>
    /// <param name="emailAddresses">A list of email addresses for the patient.</param>
    private void GetEmailStats(List<string> emailAddresses)
    {
        _emailFailures.Clear();
        _emailDeliveries.Clear();

        foreach (var emailAddress in emailAddresses)
        {
            if (emailAddress != "No email addresses on file")
            {
                var failures = TmDb.GetEmailFailureStats(emailAddress);
                _emailFailures.AddRange(failures);

                var deliveries = TmDb.GetEmailDeliveryStats(emailAddress);
                _emailDeliveries.AddRange(deliveries);
            }
        }
    }

    /// <summary>Shows the patient's email addresses in the UI.</summary>
    /// <param name="emailAddresses">A list of email addresses for the patient.</param>
    private void ShowPatientEmailAddress(List<string> emailAddresses)
    {
        if (emailAddresses.Count == 0)
        {
            emailAddresses.Add("No email addresses on file");
        }

        lblPatientEmailValue.Content = string.Join(", ", emailAddresses);
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
            {
                return "---";
            }

            var result = System.Text.RegularExpressions.Regex.Replace(value, @"\bnull\b", "<<NULL>>",  System.Text.RegularExpressions.RegexOptions.IgnoreCase);

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
            ? (statusElem.GetString() ?? string.Empty)
            : string.Empty;

        var initiatedBy = meetingDetail.Value.TryGetProperty("InitiatedBy", out var initiatedByElem)
            ? (initiatedByElem.GetString() ?? string.Empty)
            : string.Empty;

        var scheduledStart = meetingDetail.Value.TryGetProperty("ScheduledStart", out var scheduledStartElem)
            ? (scheduledStartElem.GetString() ?? string.Empty)
            : string.Empty;

        var actualStart = meetingDetail.Value.TryGetProperty("ActualStart", out var actualStartElem)
            ? (actualStartElem.GetString() ?? string.Empty)
            : string.Empty;

        var scheduledEnd = meetingDetail.Value.TryGetProperty("ScheduledEnd", out var scheduledEndElem)
            ? (scheduledEndElem.GetString() ?? string.Empty)
            : string.Empty;

        var actualEnd = meetingDetail.Value.TryGetProperty("ActualEnd", out var actualEndElem)
            ? (actualEndElem.GetString() ?? string.Empty)
            : string.Empty;

        var endedBy = meetingDetail.Value.TryGetProperty("EndedBy", out var endedByElem)
            ? (endedByElem.GetString() ?? string.Empty)
            : string.Empty;

        var joins = meetingDetail.Value.TryGetProperty("Joins", out var joinsElem)
            ? (joinsElem.GetString() ?? string.Empty)
            : string.Empty;

        var duration = meetingDetail.Value.TryGetProperty("Duration", out var durationElem)
            ? (durationElem.GetString() ?? string.Empty)
            : string.Empty;

        var meetingTitle = meetingDetail.Value.TryGetProperty("MeetingTitle", out var meetingTitleElem)
            ? (meetingTitleElem.GetString() ?? string.Empty)
            : string.Empty;

        var serviceCode = meetingDetail.Value.TryGetProperty("ServiceCode", out var serviceCodeElem)
            ? (serviceCodeElem.GetString() ?? string.Empty)
            : string.Empty;

        // Extract workflow, program, and front desk check-in
        var workflow = meetingDetail.Value.TryGetProperty("Workflow", out var workflowElem)
            ? (workflowElem.GetString() ?? string.Empty)
            : string.Empty;

        var program = meetingDetail.Value.TryGetProperty("Program", out var programElem)
            ? (programElem.GetString() ?? string.Empty)
            : string.Empty;

        var checkedInByFrontDesk = meetingDetail.Value.TryGetProperty("CheckedInByFrontDesk", out var checkedInElem)
            ? (checkedInElem.GetString() ?? string.Empty)
            : string.Empty;

        // Populate labels with null-safe values
        txbkMeetingIdValue.Text     = ReplaceNull(meetingId ?? string.Empty);
        txbkMeetingStatusValue.Text = ReplaceNull(status ?? string.Empty);
        txbkMeetingTitleValue.Text  = ReplaceNull(meetingTitle ?? string.Empty);

        // Populate meeting detail TextBlocks
        txbkMeetingStartedByValue.Text      = ReplaceNull(initiatedBy ?? string.Empty);
        txbkMeetingScheduledStartValue.Text = ReplaceNull(scheduledStart ?? string.Empty);
        txbkMeetingActualStartValue.Text    = ReplaceNull(actualStart ?? string.Empty);
        txbkMeetingScheduledEndValue.Text   = ReplaceNull(scheduledEnd ?? string.Empty);
        txbkMeetingActualEndValue.Text      = ReplaceNull(actualEnd ?? string.Empty);
        txbkMeetingEndedByValue.Text        = ReplaceNull(endedBy ?? string.Empty);
        txbkMeetingJoins.Text               = ReplaceNull(joins ?? string.Empty);
        txbkMeetingDurationValue.Text       = ReplaceNull(duration ?? string.Empty);
        txbkMeetingServiceCodeValue.Text    = ReplaceNull(serviceCode ?? string.Empty);

        // Populate additional information TextBlocks
        txbkMeetingWorkflowValue.Text             = ReplaceNull(workflow ?? string.Empty);
        txbkMeetingProgram.Text                   = ReplaceNull(program ?? string.Empty);
        txbkMeetingCheckedInByFrontDeskValue.Text = ReplaceNull(checkedInByFrontDesk ?? string.Empty);

        // Get and display meeting error if it exists
        var meetingError = TmDb.GetMeetingError(selectedMeeting.MeetingId);

        if (meetingError != null)
        {
            var kind = meetingError.Value.TryGetProperty("Kind", out var kindElem)
                ? (kindElem.GetString() ?? string.Empty)
                : string.Empty;

            var reason = meetingError.Value.TryGetProperty("Reason", out var reasonElem)
                ? (reasonElem.GetString() ?? string.Empty)
                : string.Empty;

            txbkMeetingErrorValue.Text =(!string.IsNullOrWhiteSpace(kind) || !string.IsNullOrWhiteSpace(reason))
                ? $"{kind}\n{reason}"
                : "---";
        }
        else
        {
            txbkMeetingErrorValue.Text = "---";
        }

        // Get and display participant meeting quality data from Patients.Meetings
        var qualityData     = string.Empty;
        var arrived         = string.Empty;
        var dropped         = string.Empty;
        var patientDuration = string.Empty;
        var rating          = string.Empty;
        var checkInViaChat  = string.Empty;
        var checkInWait     = string.Empty;
        var waitForCareTeam = string.Empty;
        var waitForProvider = string.Empty;
        var checkOutWait    = string.Empty;
        var device          = string.Empty;
        var os              = string.Empty;
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
                        ? mtgIdElem.GetString()
                        : null;

                    if (mtgId == selectedMeeting.MeetingId)
                    {
                        // Get all the patient meeting data
                        qualityData = meeting.TryGetProperty("QualityData", out var qualityDataElem)
                            ? (qualityDataElem.GetString() ?? string.Empty)
                            : string.Empty;

                        arrived = meeting.TryGetProperty("Arrived", out var arrivedElem)
                            ? (arrivedElem.GetString() ?? string.Empty)
                            : string.Empty;

                        dropped = meeting.TryGetProperty("Dropped", out var droppedElem)
                            ? (droppedElem.GetString() ?? string.Empty)
                            : string.Empty;

                        patientDuration = meeting.TryGetProperty("Duration", out var patientDurationElem)
                            ? (patientDurationElem.GetString() ?? string.Empty)
                            : string.Empty;

                        rating = meeting.TryGetProperty("Rating", out var ratingElem)
                            ? (ratingElem.GetString() ?? string.Empty)
                            : string.Empty;

                        checkInViaChat = meeting.TryGetProperty("CheckInViaChat", out var checkInViaChatElem)
                            ? (checkInViaChatElem.GetString() ?? string.Empty)
                            : string.Empty;

                        checkInWait = meeting.TryGetProperty("CheckInWait", out var checkInWaitElem)
                            ? (checkInWaitElem.GetString() ?? string.Empty)
                            : string.Empty;

                        waitForCareTeam = meeting.TryGetProperty("WaitForCareTeamMember", out var waitForCareTeamElem)
                            ? (waitForCareTeamElem.GetString() ?? string.Empty)
                            : string.Empty;

                        waitForProvider = meeting.TryGetProperty("WaitForProvider", out var waitForProviderElem)
                            ? (waitForProviderElem.GetString() ?? string.Empty)
                            : string.Empty;

                        checkOutWait = meeting.TryGetProperty("CheckOutWait", out var checkOutWaitElem)
                            ? (checkOutWaitElem.GetString() ?? string.Empty)
                            : string.Empty;

                        device = meeting.TryGetProperty("Device", out var deviceElem)
                            ? (deviceElem.GetString() ?? string.Empty)
                            : string.Empty;

                        os = meeting.TryGetProperty("Os", out var osElem)
                            ? (osElem.GetString() ?? string.Empty)
                            : string.Empty;

                        browser = meeting.TryGetProperty("Browser", out var browserElem)
                            ? (browserElem.GetString() ?? string.Empty)
                            : string.Empty;

                        break;
                    }
                }
            }
        }

        // Populate patient meeting detail fields
        txbkPatientArrivedValue.Text     = ReplaceNull(arrived);
        txbkPatientDroppedValue.Text     = ReplaceNull(dropped);
        txbkPatientDurationValue.Text    = ReplaceNull(patientDuration);
        txbkPatientRatingValue.Text      = ReplaceNull(rating);
        txbkCheckedInViaChatValue.Text   = ReplaceNull(checkInViaChat);
        txbkCheckInWaitValue.Text        = ReplaceNull(checkInWait);
        txbkWaitForCareTeamValue.Text    = ReplaceNull(waitForCareTeam);
        txbkWaitForProviderValue.Text    = ReplaceNull(waitForProvider);
        txbkCheckOutWaitValue.Text       = ReplaceNull(checkOutWait);
        txbkPatientDeviceValue.Text      = ReplaceNull(device);
        txbkPatientOsValue.Text          = ReplaceNull(os);
        txbkPatientBrowserValue.Text     = ReplaceNull(browser);
        txbkMeetingQualityDataValue.Text = ReplaceNull(qualityData);

        // Show/hide patient-specific and provider-specific meeting details based on current view mode
        // If we're viewing a provider, hide the patient-specific section and show provider section
        if (lblPatientProviderKey.Content?.ToString() == "PROVIDER")
        {
            brdrMeetingDetailsPatientContainer.Visibility  = Visibility.Collapsed;
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
            brdrMeetingDetailsPatientContainer.Visibility  = Visibility.Visible;
            brdrMeetingDetailsProviderContainer.Visibility = Visibility.Collapsed;
        }

        // Show the meeting details section
        spnlMeetingDetailsComponents.Visibility = Visibility.Visible;
    }


    private void DisplayMeetingResults(JsonElement? patientDetails)
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
                        ? meetingIdElem.GetString()
                        : null;

                    if (string.IsNullOrWhiteSpace(meetingId))
                    {
                        continue;
                    }

                    // Get Arrived, Dropped, Duration from Patients.Meetings
                    var arrived = meeting.TryGetProperty("Arrived", out var arrivedElem)
                        ? arrivedElem.GetString()
                        : string.Empty;

                    var dropped = meeting.TryGetProperty("Dropped", out var droppedElem)
                        ? droppedElem.GetString()
                        : string.Empty;

                    var duration = meeting.TryGetProperty("Duration", out var durationElem)
                        ? (durationElem.GetString() ?? string.Empty)
                        : string.Empty;

                    // Get ScheduledStart and Status from MeetingDetail
                    var meetingDetail  = TmDb.GetMeetingDetail(meetingId);
                    var scheduledStart = string.Empty;
                    var status         = string.Empty;

                    if (meetingDetail != null)
                    {
                        scheduledStart = meetingDetail.Value.TryGetProperty("ScheduledStart", out var startElem)
                            ? startElem.GetString()
                            : string.Empty;

                        status = meetingDetail.Value.TryGetProperty("Status", out var statusElem)
                            ? statusElem.GetString()
                            : string.Empty;
                    }

                    // Replace any occurrence of "null" (case-insensitive) with a single "---"
                    string ReplaceNull(string value)
                    {
                        if (string.IsNullOrWhiteSpace(value))
                        {
                            return "";
                        }


                        // Replace all occurrences of "null" (case-insensitive) with a placeholder
                        var result = System.Text.RegularExpressions.Regex.Replace(value, @"\bnull\b", "<<NULL>>", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

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
                        MeetingId   = meetingId,
                        Start       = ReplaceNull(scheduledStart ?? string.Empty),
                        Arrived     = ReplaceNull(arrived ?? string.Empty),
                        Dropped     = ReplaceNull(dropped ?? string.Empty),
                        Duration    = ReplaceNull(duration ?? string.Empty),
                        Status      = ReplaceNull(status ?? string.Empty),
                        HasError    = hasError,
                        IsCancelled = isCancelled,
                        IsCompleted = isCompleted
                    });
                }
            }
        }

        // Sort meetings by ScheduledStart descending (most recent first)
        meetingRows = meetingRows.OrderByDescending(m => m.Start).ToList();

        // Count meetings by status
        var totalCount     = meetingRows.Count;
        var completedCount = meetingRows.Count(m => m.IsCompleted);
        var cancelledCount = meetingRows.Count(m => m.IsCancelled);

        // Count In-Progress, Expired, and Scheduled
        var inProgressCount = 0;
        var expiredCount    = 0;
        var scheduledCount  = 0;

        foreach (var meeting in meetingRows)
        {
            var statusLower = meeting.Status?.ToLower() ?? string.Empty;

            // Skip already counted statuses
            if (meeting.IsCompleted || meeting.IsCancelled)
            {
                continue;
            }

            if (statusLower.Contains("in progress") || statusLower.Contains("in-progress"))
            {
                inProgressCount++;
            }
            else if (statusLower.Contains("expired"))
            {
                expiredCount++;
            }
            else if (statusLower.Contains("scheduled"))
            {
                scheduledCount++;
            }
        }

        // Update the header with the detailed count using individual TextBlocks
        txbkTotalMeetingsValue.Text      = $"{totalCount} MEETINGS";
        txbkCompletedMeetingsValue.Text  = $"{completedCount} Completed";
        txbkMeetingsInProgressValue.Text = $"{inProgressCount} In-Progress";
        txbkMeetingsExpiredValue.Text    = $"{expiredCount} Expired";
        txbkMeetingsCancelledValue.Text  = $"{cancelledCount} Cancelled";
        txbkMeetingsScheduledValue.Text  = $"{scheduledCount} Scheduled";

        // Bind to DataGrid
        dgrdMeetingResults.ItemsSource = meetingRows;

        // Show meetings section if there are meetings
        spnlMeetingComponents.Visibility = meetingRows.Count > 0
            ? Visibility.Visible
            : Visibility.Collapsed;

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