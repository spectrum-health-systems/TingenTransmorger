// 260219_code
// 260219_documentation

using System.Text.Json;
using System.Windows;
using TingenTransmorger.Models;

namespace TingenTransmorger;

/* The MainWindow.MeetingDetails partial class contains logic related to displaying meeting details in the UI.
 */
public partial class MainWindow : Window
{

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





























}